# UAG Bridge — qFoldIT Toolbelt for UNIGINE 2 / UNIGINE 2 Sim

The UAG Bridge is what connects UNIGINE-TOOLBELT to the rest of the
qFoldIT stack (`SOS → SKG → SEM → UAG → UWI → MCP`), companion to (not a
replacement for) UNIGINE's official MCPBridge Plugin. As of this revision
it conforms to the **formal, normative** artifacts in
`qfoldit-engine-adapter-spec-v0.1`, not the earlier informal markdown
schema draft this bridge was originally built against:

- `editor_plugin/UagModel.cs` — matches `schemas/uag.schema.json` exactly,
  identical shape to UNITY-TOOLBELT's version, so a single UAG document
  is byte-for-byte interchangeable between the two engine adapters.
- `editor_plugin/UagValidator.cs` — emits `{code, message}` errors
  matching `conformance/test_vectors.json` (verified against the actual,
  unmodified vector file — see `tests/conformance/`).
- `qfoldit.adapter.json` (repo root) — strictly valid against
  `schemas/adapter-manifest.schema.json`.
- `editor_plugin/Tools/UAGBridgeTools.cs` — `uag_validate`/`uag_apply`
  return shape matches `schemas/execution-report.schema.json`.

## Schema, in brief

See UNITY-TOOLBELT's `docs/UAG_BRIDGE.md` for the full schema walkthrough
— identical here. Key points: `node.parent` (not `parent_id`), no
`connections[]` array, a `bindings[]` array for scientific-state
attachments.

## The two tools

### `uag_validate({ "uag_json": "..." })`

Same contract as the Unity version: `INVALID_SCHEMA`, `DUPLICATE_NODE_ID`,
`DANGLING_PARENT`/`DANGLING_REFERENCE`, `HIERARCHY_CYCLE`, plus gap
reporting (informational).

### `uag_apply({ "uag_json": "..." })`

Validates first; **aborts with zero `ToolRegistry.Dispatch` calls if
invalid** (verified directly in `tests/uag_bridge_simulation/`). Five
passes: nodes → `parent` hierarchy → `constraints[]` → `interactions[]`
(real realization, see below) → `bindings[]` (real realization, see
below). Returns the same structured execution report shape as the Unity
adapter (`status`/`created`/`updated`/`skipped`/`gaps`/`warnings`/
`errors`/`provenance`).

## Node type → tool mapping

| UAG `type` | UNIGINE tool(s) dispatched | Notes |
|---|---|---|
| `mesh` | `asset_instantiate_node` if `properties.mesh_ref` set, else `spawn_primitive` | |
| `light` | `light_create` | |
| `camera` | `camera_create` | |
| `audio_source` | `spawn_group_node` + `audio_add_source` | **requires** `properties.sound_path` — fails cleanly with no orphan node if missing |
| `particle_emitter` | `particles_spawn_from_asset` | **requires** `properties.asset_ref` |
| `ui_panel` | `ui_create_panel` | |
| `trigger_volume` | `spawn_primitive` + `physics_add_shape(is_trigger=true)` | |
| `group` | `spawn_group_node` (`NodeDummy`) | |
| `molecular_structure` | `scientific_visualization_create` | legacy type from the spec's own hand-authored example |
| `interaction_zone` | `spawn_primitive` + trigger shape + `interaction_create` | legacy type; `properties.interaction` selects the interaction type |
| `scientific_subject/<mechanic>` | `scientific_visualization_create` | **the exact shape `reference/compiler.py` emits** |
| `custom` | *(none)* | always unmapped |

## Real capability: `interaction` and `scientific.visualization`

Same P0 priorities as the Unity adapter, realized honestly within this
engine's real constraints:

- **`interaction_create`** (`editor_plugin/Tools/InteractionTools.cs`)
  ensures the target node has a real physics shape/body — so
  `physics_raycast_query` can genuinely detect it — and records the
  interaction type in a persisted JSON registry
  (`Saved/QFoldIT_Toolbelt/interactions.json`), readable back via
  `interaction_get`/`interaction_list`. Covers all 10 gameplay mechanics
  plus legacy triggers.
- **`scientific_visualization_create`**
  (`editor_plugin/Tools/ScientificVisualizationTools.cs`) realizes a
  `scientific_subject/<mechanic>` node as a real, visible,
  mechanic-differentiated primitive (shape + material preset keyed by
  mechanic).
- **`scientific_binding_create`** records the bound `scientific-state://`
  URI in a persisted, queryable JSON registry
  (`Saved/QFoldIT_Toolbelt/scientific_bindings.json`, readable via
  `scientific_binding_get`), instead of silently accepting-and-discarding
  the binding.

**Honest scope — deliberately more conservative than the Unity adapter**,
because UNIGINE's Input/callback and 3D-text/billboard APIs need
SDK-version verification this adapter doesn't have access to (the same
constraint flagged throughout this repo since Phase 1):

- **No live click-to-callback.** Unity's `Runtime/QFoldITInteractable.cs`
  has a real, working `OnMouseDown → UnityEvent` wiring. This adapter
  gives you real physical selectability (`physics_raycast_query` will hit
  the node) and a real, queryable interaction-type record — but does
  **not** itself fire a callback when clicked. A companion script polling
  input and cross-referencing `interaction_get` against
  `physics_raycast_query`'s hit result is the documented path to a live
  callback; it isn't implemented here rather than guessed at.
- **No floating label.** Unity's version optionally adds a world-space
  text label above the visualization anchor. This adapter does not,
  since UNIGINE's 3D text API wasn't available to verify.

`uag_apply` emits an explicit `warning` for every realized
gameplay-mechanic interaction spelling this out, rather than letting
`status: "success"` imply more than was actually delivered.

## Known gaps (reported, not hidden)

- **`audio_source` / `particle_emitter` without an asset reference**: type
  is mapped, a specific instance fails cleanly at apply time (no orphan
  node — verified directly in `tests/uag_bridge_simulation/`) if
  `properties.sound_path`/`properties.asset_ref` is missing.
- **`joint_slider`**: `physics_add_joint`'s constructor signatures were
  never verified against a live SDK (see `PhysicsTools.cs`'s header) —
  kept at capability status `partial` in `qfoldit.adapter.json`, not
  `supported`, specifically because of this.
- **`data_link` and `logic_rule`-flavoured constraints**: no live UNIGINE
  primitive — reported as gaps.

## Verified

- `editor_plugin/UagModel.cs`/`UagValidator.cs`/`UAGBridgeMechanics.cs`
  have zero `Unigine.*` dependency by design — compiled and run standalone
  with `mcs`/`mono`, including against the **real, unmodified**
  `conformance/test_vectors.json` from `qfoldit-engine-adapter-spec-v0.1`.
- `editor_plugin/Tools/UAGBridgeTools.cs` dispatches through
  `ToolRegistry.Dispatch(name, JObject)` for every call — the **entire
  orchestration logic**, including the new `scientific_subject/*`/
  interaction/binding passes, was compiled and run end-to-end against fake
  tool handlers standing in for the real UNIGINE-backed tools (22
  assertions, `tests/uag_bridge_simulation/`), confirming exactly which
  tools get called, with what parameters, in what order, and that an
  invalid graph produces **zero** dispatch calls.
- **The compiled result**: running the spec's own unmodified
  `reference/compiler.py` against this repo's actual `qfoldit.adapter.json`
  compiles all 5 currently-unlocked gameplay patterns with
  `status=success` and zero gaps — even with `physics.joints` honestly
  kept at `partial`.
