# UAG Bridge — qFoldIT Toolbelt for UNIGINE 2 / UNIGINE 2 Sim

The UAG Bridge is what makes UNIGINE-TOOLBELT part of the qFoldIT stack
(`SOS → SKG → SEM → UAG → UWI → MCP`) rather than a standalone automation
kit sitting next to MCPBridge Plugin. It's two tools, `uag_validate` and
`uag_apply`, implemented in
[`editor_plugin/Tools/UAGBridgeTools.cs`](../editor_plugin/Tools/UAGBridgeTools.cs),
built on the UAG v0.1 model in
[`editor_plugin/UagModel.cs`](../editor_plugin/UagModel.cs) and the
validator in [`editor_plugin/UagValidator.cs`](../editor_plugin/UagValidator.cs).

Same schema as UNITY-TOOLBELT's bridge — copied field-for-field from the
canonical source (`qfoldit/UEFN-TOOLBELT`'s
`.claude/skills/game-designer/references/uag_schema.md`), so a single UAG
document works against either engine adapter unchanged.

## Design principle — and one difference from the Unity version

Same rule as UEFN-TOOLBELT's `unreal-world-builder` skill and
UNITY-TOOLBELT's own bridge: **never re-implement a primitive here.**
What's different structurally: this file dispatches through
`ToolRegistry.Dispatch(name, JObject)` for *every* tool call, instead of
calling other files' methods directly in-process. That's the exact same
path an external MCP client uses via `mcp_server.py`'s
`run_toolbelt_tool` — so this adapter has no special access any other
caller lacks, and its whole orchestration logic could be (and was, see
below) tested by swapping in fake handlers for the dispatched tool names.

## The two tools

### `uag_validate({ "uag_json": "..." })`

Identical contract to the Unity version: duplicate-id check, dangling
reference check (`parent_id` / `from_node` / `to_node` / `target_nodes[]`
/ interaction `target_node`), `parent_child` cycle detection, and a gap
report (`unmapped_node_types`, `unmapped_constraint_types`,
`unmapped_interactions` — informational, not errors).

### `uag_apply({ "uag_json": "...", "generate_interaction_stub": true, "stub_output_path": "..." })`

Validates first; **aborts with zero `ToolRegistry.Dispatch` calls if
invalid** (verified directly — see below). If valid, four passes:

1. Create every mapped-type node via `Dispatch`.
2. Apply `parent_id` hierarchy via `parent_node`.
3. Apply `connections[]`: `parent_child` → `parent_node`; `joint_fixed` /
   `joint_hinge` / `joint_slider` → `physics_add_joint`; anything else
   (`data_link`) → `unmapped_connection_types`.
4. Apply `constraints[]`: `physics_collision` → `physics_add_shape` +
   `physics_add_body`; anything else is collected for the interaction stub.

Every node touched by an unmapped constraint or any interaction is
collected into one set; if non-empty and `generate_interaction_stub` is
true, `uag_apply` dispatches `codegen_node_component` once to produce a
`UagInteractionHandlers` WorldLogic class with a real node reference per
target — a usable artifact, not just a text report.

## Node type → tool mapping

| UAG `type` | UNIGINE tool(s) dispatched | Notes |
|---|---|---|
| `mesh` | `asset_instantiate_node` if `properties.mesh_ref` set, else `spawn_primitive` | `properties.primitive` selects the shape (default `box`) |
| `light` | `light_create` | `properties.light_type`, `color_hex`, `intensity` |
| `camera` | `camera_create` | `properties.fov` |
| `audio_source` | `spawn_group_node` (anchor) + `audio_add_source` | **requires** `properties.sound_path` — UNIGINE has no default sound asset; missing it is a per-node failure, not a type-level gap (see below) |
| `particle_emitter` | `particles_spawn_from_asset` | **requires** `properties.asset_ref` (a `.particles` file) — UNIGINE has no built-in generic preset the way Unity's `ParticleSystem` defaults do |
| `ui_panel` | `ui_create_panel` | world x/y truncated to `int` and reused as 2D screen position |
| `trigger_volume` | `spawn_primitive` (box) + `physics_add_shape(is_trigger=true)` | |
| `group` | `spawn_group_node` (`NodeDummy`) | added specifically to close this gap — UNIGINE's real empty-container node type |
| `custom` | *(none)* | always unmapped |

## Known gaps (reported, not hidden)

- **`audio_source` / `particle_emitter` without an asset reference**: the
  *type* is mapped, but a specific node instance fails at apply time if
  `properties.sound_path` / `properties.asset_ref` is missing — this shows
  up in `node_failures`, not `unmapped_node_types`. Verified directly in
  the simulation test (see below): a `snd1` node with no `sound_path`
  correctly appears in `node_failures` while everything else still applies.
- **`joint_slider`**: `physics_add_joint`'s constructor uses `JointSlider`
  if available on your SDK version, but per the `⚠` note in
  `PhysicsTools.cs`, exact joint constructor signatures vary by SDK — this
  is flagged as an execution-time risk, not silently assumed to work.
- **Non-uniform scale**: same approximation as the Unity bridge — a UAG
  `scale: [2, 1, 0.5]` is applied via `transform_node`'s single `scale`
  float (its X component only).
- **`data_link` connections** and **`interaction_grabbable` /
  `animation_trigger` / `logic_rule` constraints / all interaction
  triggers**: no live UNIGINE primitive — always become gaps, turned into
  the `UagInteractionHandlers` codegen stub.

## How this was verified

Both `UagModel.cs`/`UagValidator.cs` (zero `Unigine.*` dependency by
design) and — going further than the Unity version — the **entire
`uag_apply` orchestration logic** were compiled and run standalone with
`mcs`/`mono`, outside any UNIGINE installation:

- `tests/uag_validator/` — the same 10-scenario/24-assertion validator
  coverage as Unity's, run against this file's actual
  `MappedNodeTypes`/`MappedConstraintTypes` sets.
- `tests/uag_bridge_simulation/` — fake handlers registered in place of
  the real `Unigine.*`-backed tools, then `uag_apply` run against a graph
  covering every node type (including one deliberately unmapped `custom`
  node and one node that fails at apply time), every connection type
  (including unmapped `data_link`), every constraint type (including
  unmapped `logic_rule`), and an interaction — 23 assertions checking both
  the returned report **and** the exact sequence of tool names/parameters
  dispatched. Also verifies the abort contract directly: an invalid graph
  produces zero `Dispatch` calls, not a partial application.

This is real confidence in the bridge's control flow — what it is *not* a
substitute for is verifying that the real `spawn_primitive` /
`light_create` / etc. implementations behave as documented against a live
UNIGINE SDK; see the `⚠` notes throughout `editor_plugin/Tools/*.cs` for
what still needs checking there.
