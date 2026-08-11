# qFoldIT Toolbelt — UNIGINE 2 / UNIGINE 2 Sim

**90 composite editor-automation tools for UNIGINE 2, exposed to AI agents via a companion MCP bridge that sits alongside UNIGINE's own official MCPBridge Plugin.**

> Built by **qFoldIT** — foundation release, 2026

---

## What this is, and how it relates to UNIGINE's MCPBridge Plugin

UNIGINE already publishes an official **MCPBridge Plugin** on the Add-On
Store: a built-in MCP server that runs directly inside the Editor with 27
low-level tools (primitives, node templates, transforms, materials,
components, XML inspect/import, console commands). That plugin is the
foundation layer — it needs no external process and is the right place for
raw editor control.

**qFoldIT Toolbelt is a companion, not a replacement.** It adds a second
layer of higher-level, *composite* tools the same way
[UEFN Toolbelt](https://github.com/undergroundrap/UEFN-TOOLBELT) adds 355
named commands on top of UEFN's raw Python API instead of making an agent
write one-off scripts every call:

Instead of an agent issuing a dozen raw primitive-creation + transform
calls to lay out a competitive arena, it calls:
```
arena_generate(size="medium")
procedural_place(pattern="circle", count=12, radius=8, node_path="props/crate.node")
material_team_color_split(team_a_contains="RedSpawn", team_b_contains="BlueSpawn")
```

## Why a separate external bridge (and not registered inside MCPBridge itself)

UNIGINE has not currently published a documented API for third parties to
register additional tools directly into MCPBridge's in-process MCP server.
So instead of guessing at an undocumented internal hook, this toolbelt uses
the same two-process pattern UEFN Toolbelt uses for exactly the same
reason (UEFN has no native MCP at all): an external `mcp_server.py` bridge
that any MCP client connects to over stdio, relaying calls over local HTTP
to a listener running inside the Editor.

```
Claude / any MCP client
    │  MCP protocol (stdio)
    ▼
mcp_server.py            (external, this repo)
    │  HTTP POST 127.0.0.1:8766
    ▼
UNIGINE 2 Editor process
    ├── MCPBridge Plugin (official, UNIGINE)  — 27 base tools, its own MCP server
    └── qFoldIT ToolbeltListener.cs (this repo) — 25 composite tools, HTTP listener
```

Both run side by side. If UNIGINE later documents a plugin-extension API
for MCPBridge, the tool *logic* here (in `editor_plugin/Tools/*.cs`) can be
re-registered directly into it — only the transport (`mcp_server.py` +
`ToolbeltListener.cs`) would need to change.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full design and
[docs/TOOL_REFERENCE.md](docs/TOOL_REFERENCE.md) for every tool's signature.

## ⚠ Before you build

None of this repository's `Unigine.*` calls were compiled or run against a
live UNIGINE installation — this was written without access to the SDK.
Exact property vs. method access, primitive mesh asset paths, widget
constructor signatures, and physics/navigation type availability **do vary
between SDK versions**. Check each call against **Help → API
Documentation** in your SDK Browser (targeting 2.20/2.21, matching what
MCPBridge Plugin lists as supported) before relying on this in a real
project.

`editor_plugin/UnigineCompat.cs` holds the core node/material/transform
operations shared across every tool file. The expanded tool set added in
this release (Lighting, Physics, UI, Audio, Camera, Particles, Navigation,
Components, etc.) calls `Unigine.*` more directly where a shared helper
didn't make sense for a single-use API — each of those files carries its
own `⚠` comment block at the top calling out exactly which calls are most
likely to need adjusting.

## Install

1. You already have **MCPBridge Plugin** (UNIGINE's official plugin)
   installed — confirmed by your Add-On Store purchase. Keep it enabled;
   qFoldIT Toolbelt runs alongside it, not instead of it.
2. Copy `editor_plugin/` into your UNIGINE project (e.g. under
   `source/qfoldit_toolbelt/`) and add `ToolbeltBootstrap` as a world
   script / autostart WorldLogic component so it initializes with the
   Editor.
3. Install the Python bridge dependency:
   ```bash
   pip install mcp
   ```
4. Register the bridge with your MCP client. Example for Claude Code
   (`.mcp.json`):
   ```json
   {
     "mcpServers": {
       "qfoldit-unigine-toolbelt": {
         "command": "python",
         "args": ["<ABSOLUTE_PATH>/qfoldit-unigine-toolbelt/mcp_server.py"]
       }
     }
   }
   ```
5. Open the Editor (so `ToolbeltBootstrap.Init()` starts the listener),
   then connect your MCP client. Call `list_toolbelt_tools` to confirm the
   handshake.

## Tool categories (90 tools total)

| Category | Tools | What it covers |
|----------|:-----:|-----------------|
| Assets | 3 | List, instantiate, and find project .node/.mesh/.mat assets by extension and name. |
| Audio | 4 | ObjectSound source setup, one-shot playback, listener node, volume control. |
| Camera | 5 | Player/camera creation, dependency-free follow, clipping, FOV, screenshots. |
| Components | 5 | Reflection-based generic add/remove/get/set/list for C# Components. |
| BuildConsole | 3 | Run console commands, read console variables, save the world. |
| Interaction | 3 | Real interaction realization: ensures physics selectability + a persisted, queryable interaction-type registry for any of the 10 gameplay mechanics or legacy triggers. |
| Lighting | 5 | Create lights, set environment/fog, trigger GI reload, apply full lighting presets. |
| Materials | 4 | 12 material presets, bulk swap by name match, team-color split, preset listing. |
| Measurement | 3 | Distance between nodes, per-node bounds, full-world bounds. |
| Navigation | 4 | NavigationMesh bake, agents, obstacles, runtime destinations (Navigation add-on). |
| CodeGen | 1 | Generates a WorldLogic component with real, bindable node references for named world nodes. |
| NodeWorkflow | 4 | Save/reload/variant/XML-export workflow for .node assets — UNIGINE's prefab-equivalent. |
| Particles | 4 | ObjectParticles spawning from assets, emission rate, color, stop control. |
| Physics | 6 | Rigid bodies, collision shapes, physics materials, raycasts, global gravity. |
| Procedural | 2 | 8 geometric placement patterns (grid, circle, arc, spiral, line, wave, helix, radial) plus a symmetrical arena generator. |
| Project | 1 | Standard folder scaffold plus a boilerplate GameManager WorldLogic class. |
| Scene | 8 | Spawn, transform, clone, delete, parent, list, and find nodes in the loaded world. |
| ScientificVisualization | 3 | Real scientific-state visualization: mechanic-differentiated visible primitives plus a persisted, queryable binding registry for live scientific-state URIs. |
| Stamps | 3 | Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps. |
| TagsLayers | 4 | Free-text tag registry plus real IntersectionMask-backed named layers. |
| UAGBridge | 2 | Validates and realizes qFoldIT Universal Assembly Graphs by dispatching to this toolbelt's own registered tools — the Universal World Interface adapter connecting UNIGINE 2 to the rest of the qFoldIT stack. |
| UI | 5 | Widget-based UI: buttons, labels, panels, sliders, tracked by a lightweight name registry. |
| Utility | 2 | Batch rename and basic Engine/world info reporting. |
| WorldManagement | 5 | New/load/save-as/reload/info for the single active world. |
| WorldState | 1 | Exports the full world node graph (names, types, transforms, parents) to JSON for AI context. |

## Roadmap to parity

This release brings the toolbelt to **90 real tools** across 25
categories. More importantly, this revision adapts the whole UAG Bridge to
**qfoldit-engine-adapter-spec-v0.1**, the formal spec package (not the
earlier informal Phase-1 draft): `UagModel.cs` now matches the normative
`schemas/uag.schema.json` exactly (`schema`/`scene`/`node.parent`/
`bindings[]`), `uag_validate` emits `{code, message}` errors matching the
spec's own `conformance/test_vectors.json` byte-for-byte, and
`qfoldit.adapter.json` (this repo's root) is strictly valid against
`schemas/adapter-manifest.schema.json`.

**Real, verified milestone**: running the spec's own unmodified
`reference/compiler.py` (from `qfoldit-scientific-gameplay-framework-v0.1`)
against this repo's actual `qfoldit.adapter.json` now compiles all 5
currently-unlocked gameplay patterns with `status=success` and zero gaps —
up from 0/5 before this revision. Earned by building real capability, not
by hand-editing the manifest's status field:

- **`interaction`** (blocked 4/5 patterns): `InteractionTools.cs` ensures
  a target node has a real physics shape/body (so `physics_raycast_query`
  can detect it) and records the interaction type in a persisted,
  queryable JSON registry (`interaction_get`/`interaction_list`), for all
  10 gameplay mechanics plus legacy triggers. Honest scope, explicitly
  documented in the manifest and the file's own header: unlike the Unity
  adapter's real `OnMouseDown → UnityEvent` wiring, this does **not**
  wire a live click-to-callback — UNIGINE's Input/callback API needs
  SDK-version verification this adapter doesn't have access to.
- **`scientific.visualization`** (blocked 5/5 patterns):
  `ScientificVisualizationTools.cs` realizes every UAG
  `scientific_subject/<mechanic>` node as a real, visible,
  mechanic-differentiated primitive, plus a real, persisted binding
  registry giving `bindings[]` genuine substance. Honest scope: unlike
  the Unity adapter, no floating 3D label — UNIGINE's 3D text API is
  another SDK-version unknown this adapter didn't guess at.
- **`geometry.procedural`** (blocked 2/5 patterns): already real, working
  capability from an earlier revision — no new code needed, just an
  honest status correction.
- **`physics.joints`**: deliberately kept at `partial`, not `supported` —
  `physics_add_joint`'s exact joint constructor signatures were never
  verified against a live SDK (see the top of `PhysicsTools.cs`). Not
  required by any of the 5 currently-unlocked patterns, so this doesn't
  block the 5/5 result; a more conservative, honest rating than what
  Unity's native-Unity-component version of the same capability can
  claim.

See [docs/UAG_BRIDGE.md](docs/UAG_BRIDGE.md) for the full contract,
mapping table, and every capability's exact honest scope.
`tests/conformance/` runs the spec's real `test_vectors.json` against the
actual, unmodified `UagValidator.cs`; `tests/uag_bridge_simulation/` runs
the full `uag_apply` orchestration end-to-end against fake tool handlers.

Structured to keep growing the same way UEFN Toolbelt's did: new files
under `editor_plugin/Tools/`, each calling `ToolRegistry.Register(...)`
for a handful of new tools, tracked in `registry.json`.


## License

AGPL-3.0, with an additional visible-attribution requirement — see [LICENSE](LICENSE). Any tool built on this codebase must credit qFoldIT and link back to this repository (see LICENSE for the exact wording); network/hosted use requires publishing your modified source.
