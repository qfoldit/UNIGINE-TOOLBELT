# Architecture — qFoldIT Toolbelt for UNIGINE 2 / UNIGINE 2 Sim

## Two MCP servers, one Editor process

Unlike the Unity toolbelt (which registers directly into Unity's own
in-process `McpToolRegistry`), the UNIGINE Editor ends up running **two**
independent MCP-reachable surfaces at once:

1. **MCPBridge Plugin** (official, UNIGINE) — its own built-in MCP server,
   27 low-level tools, no external process.
2. **qFoldIT Toolbelt** (this repo) — an external `mcp_server.py` bridging
   over local HTTP to `ToolbeltListener.cs` running inside the same Editor
   process.

An MCP client that wants both simply connects to both servers — most
clients (Claude Code included) support multiple `mcpServers` entries
simultaneously. There is no conflict: MCPBridge listens on its own
transport, `ToolbeltListener` listens on `127.0.0.1:8766` by default
(configurable via `UNIGINE_TOOLBELT_PORT`).

```
AI Client (Claude Code, Cursor, ...)
    │
    ├── MCP over MCPBridge's transport ──► MCPBridge Plugin (27 tools)
    │
    └── MCP over stdio ──► mcp_server.py ──► HTTP 127.0.0.1:8766 ──► ToolbeltListener.cs (25 tools)
                                                                            │
                                                                      ToolRegistry.Dispatch()
                                                                            │
                                                              editor_plugin/Tools/*.cs
                                                                            │
                                                                    UnigineCompat.cs
                                                                            │
                                                                       Unigine.* API
```

## Why the HTTP-relay pattern instead of reflection-based discovery

Unity's `[McpTool]` attribute + `TypeCache` scan works because Unity MCP
documents that discovery mechanism publicly. UNIGINE's MCPBridge does not
currently document an equivalent extension point, so this toolbelt cannot
assume one exists. Two options were available:

- **Wait / guess at an undocumented internal API** — fragile, likely to
  break silently across MCPBridge point releases.
- **Reuse UEFN Toolbelt's proven two-process pattern** — an external MCP
  server that any client already knows how to talk to, relaying to a
  small in-Editor HTTP listener. This is exactly the pattern UEFN Toolbelt
  uses for the same underlying reason (no native MCP registration hook),
  and it is easy to swap out later: everything in `editor_plugin/Tools/`
  is plain C# with no dependency on the HTTP transport itself.

`ToolbeltListener.cs` is deliberately transport-only — it has zero
`Unigine.*` references and should compile against any C# runtime UNIGINE
embeds without modification. All engine-specific logic lives below it.

## Main-thread safety

`HttpListener.GetContext()` blocks on a background thread. World/Node/
Material API calls in UNIGINE are not guaranteed safe to call off the
main simulation thread, so every incoming request is queued
(`ConcurrentQueue<PendingRequest>`) and only executed when
`ToolbeltBootstrap.Update()` calls `PumpMainThread()` once per frame. The
HTTP thread blocks on a `ManualResetEventSlim` until the main thread has
produced a result, then writes the HTTP response — keeping the external
client's request/response cycle synchronous even though execution hops
threads internally.

## File layout

```
qfoldit-unigine-toolbelt/
├── mcp_server.py                  external stdio↔HTTP MCP bridge
├── requirements.txt
├── editor_plugin/
│   ├── ToolbeltBootstrap.cs       WorldLogic entry point: starts + pumps the listener, applies camera follow
│   ├── ToolbeltListener.cs        HTTP transport, main-thread dispatch queue (engine-agnostic)
│   ├── ToolRegistry.cs            name -> handler dispatch table
│   ├── UnigineCompat.cs           ⚠ the file with direct Unigine.* API calls for core node/material/transform ops
│   ├── UagModel.cs                UAG v0.1 data model (POCO, zero Unigine.* dependency)
│   ├── UagValidator.cs            dangling-ref/cycle/gap validation (zero Unigine.* dependency)
│   └── Tools/
│       ├── SceneTools.cs          spawn/transform/clone/delete/parent/list/find
│       ├── MaterialTools.cs       presets, bulk swap, team-color split
│       ├── ProceduralPlacementTools.cs   8-pattern placement + arena generator
│       ├── StampTools.cs          save/place/list reusable node groups
│       ├── ProjectSetupTools.cs   folder scaffold + GameManager boilerplate
│       ├── WorldStateExportTools.cs      world node graph → JSON
│       ├── NodeCodeGenTools.cs    WorldLogic component wired to real nodes
│       ├── AssetTools.cs         list/instantiate/find project assets
│       ├── ConsoleTools.cs       console commands, variables, world save
│       ├── LightingTools.cs      lights, environment/fog, GI reload, presets
│       ├── PhysicsTools.cs       rigid bodies, shapes, materials, raycasts, gravity
│       ├── UITools.cs            Widget-based buttons/labels/panels/sliders
│       ├── AudioTools.cs         ObjectSound source, one-shots, listener, volume
│       ├── CameraTools.cs        Player/camera creation, follow, clipping, FOV, screenshots
│       ├── ParticleTools.cs      ObjectParticles spawn/emission/color/stop
│       ├── NavigationTools.cs    NavigationMesh bake, agents, obstacles, destinations
│       ├── NodeWorkflowTools.cs  .node save/reload/variant/XML export (prefab-equivalent)
│       ├── ComponentTools.cs     reflection-based generic C# Component add/remove/get/set/list
│       ├── TagsLayersTools.cs    free-text tags + IntersectionMask-backed layers
│       ├── WorldManagementTools.cs   new/load/save-as/reload/info for the active world
│       ├── MeasurementTools.cs   distance, per-node bounds, world bounds
│       ├── UtilityTools.cs       batch rename, engine/world info
│       └── UAGBridgeTools.cs     uag_validate / uag_apply — see docs/UAG_BRIDGE.md
├── docs/TOOL_REFERENCE.md
├── docs/UAG_BRIDGE.md             UAG contract, mapping table, verification notes
├── registry.json                  plugin manifest (mirrors UEFN Toolbelt's format)
└── tests/
    ├── test_mcp_server.py         mcp_server.py contract tests (no live Editor required)
    ├── uag_validator/              standalone mono tests for UagValidator.cs (see its README)
    └── uag_bridge_simulation/      standalone mono end-to-end simulation of uag_apply (see its README)
```

## Tool authoring convention

Every tool follows the same shape:

```csharp
ToolRegistry.Register("my_tool", "CategoryName",
    "One-line description shown to the AI client.",
    MyToolHandler);

private static object MyToolHandler(JObject p)
{
    // read params via p["field"], call into UnigineCompat, never Unigine.* directly
    return new { success = true, /* structured result */ };
}
```

Rules kept consistent across all tool files:

- **Always return a structured object** with at least a `success` boolean
  — never throw for expected failure cases (node not found, unknown
  preset, etc.); return `{ success = false, error = "..." }` instead so an
  agent can branch on it.
- **Find nodes by name, not internal ID**, for parity with how an agent
  reads `world_list_nodes` / `world_state_export` output back into
  subsequent calls.
- **One tool per capability, not per variant** — `procedural_place` takes
  a `pattern` string instead of eight separate tools, matching both the
  Unity toolbelt and UEFN Toolbelt's `Prop Patterns` design.
- **Shared node/material/transform operations go through `UnigineCompat.cs`**
  where practical; tool files with a single-use engine API (Lighting, Physics,
  UI, Audio, Navigation, etc.) call `Unigine.*` directly but each carries its
  own `⚠` comment block flagging exactly which calls to re-verify per SDK
  version — see the "Before you build" section in README.md.

## Extending the toolbelt

To add a new tool: create or extend a file under `editor_plugin/Tools/`,
call `ToolRegistry.Register(...)` for it, add the call to
`ToolRegistry.RegisterAll()` if it's a new file, and add an entry to
`registry.json`. Unlike Unity, there is no reflective auto-discovery here
— registration is explicit by design (see rationale above).

Planned next categories (not yet implemented): terrain/landscape tools,
particle system presets, vehicle/character controller scaffolding,
VR/multi-display config helpers (relevant to UNIGINE 2 Sim specifically),
and — if UNIGINE documents one — direct MCPBridge tool registration to
retire the external HTTP relay.
