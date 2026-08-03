# qFoldIT Toolbelt — UNIGINE 2 / UNIGINE 2 Sim

**25 composite editor-automation tools for UNIGINE 2, exposed to AI agents via a companion MCP bridge that sits alongside UNIGINE's own official MCPBridge Plugin.**

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

`editor_plugin/UnigineCompat.cs` is the **only** file that calls
`Unigine.*` types directly for node/material/transform operations. Its
method bodies reflect the commonly documented UnigineSharp shape for the
2.20/2.21 line (matching what MCPBridge Plugin lists as its supported
SDK), but exact property vs. method access, primitive mesh asset paths,
and the Editor selection API **do vary between SDK versions and were not
compiled or run against a live UNIGINE installation to produce this
repository**. Check each call against **Help → API Documentation** in your
SDK Browser before relying on this in a real project — see the comments at
the top of `UnigineCompat.cs` and `StampTools.cs` for the specific calls
most likely to need adjustment.

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

## Tool categories (25 tools total)

| Category     | Tools | What it covers |
|--------------|:-----:|-----------------|
| Scene        | 7 | spawn, transform, clone, delete, parent, list, find nodes |
| Materials    | 4 | 12 presets, bulk swap, team-color split, list presets |
| Procedural   | 2 | 8-pattern placement, symmetrical arena generator |
| Stamps       | 3 | save/place/list reusable node groups |
| Project      | 1 | folder scaffold + boilerplate GameManager WorldLogic |
| WorldState   | 1 | full world node graph → JSON for AI context |
| CodeGen      | 1 | WorldLogic component wired to real world nodes |
| Assets       | 3 | list / instantiate / find .node & other project assets |
| BuildConsole | 3 | run console commands, read variables, save world |

## Roadmap to parity

This is a **foundation release** — 25 real tools, not 355 or MCPBridge's
27. Structured to grow the same way UEFN Toolbelt's did: new files under
`editor_plugin/Tools/`, each calling `ToolRegistry.Register(...)` for a
handful of new tools, tracked in `registry.json`.

## License

AGPL-3.0, with an additional visible-attribution requirement — see [LICENSE](LICENSE). Any tool built on this codebase must credit qFoldIT and link back to this repository (see LICENSE for the exact wording); network/hosted use requires publishing your modified source.
