"""
qFoldIT Toolbelt for UNIGINE 2 / UNIGINE 2 Sim — mcp_server.py
================================================================
External FastMCP bridge that connects an MCP client (Claude Code, etc.)
to the UNIGINE 2 Editor.

WHY A SEPARATE BRIDGE, GIVEN UNIGINE ALREADY HAS AN OFFICIAL MCPBridge PLUGIN
-------------------------------------------------------------------------
UNIGINE's own "MCPBridge Plugin" (published by UNIGINE on the Add-On Store)
already runs a built-in MCP server directly inside the Editor with 27 tools
covering primitives, templates, transforms, materials, components, and
console access — no external process required for that base layer.

qFoldIT Toolbelt is a COMPANION layer, not a replacement: it adds
higher-level, *composite* tools (arena generation, procedural placement
patterns, reusable stamps, project scaffolding, node code generation) the
same way UEFN Toolbelt adds 355 composite commands on top of UEFN's raw
Python API instead of making an agent write one-off scripts every call.

Because UNIGINE's official plugin does not currently document a public API
for third parties to register additional tools directly into its in-process
MCP server, this bridge follows the proven, documented UEFN Toolbelt
pattern instead: an external stdio MCP server here, relaying HTTP calls to
a listener running inside the Editor (editor_plugin/ToolbeltListener.cs).
If/when UNIGINE publishes a plugin-extension API for MCPBridge, the
qFoldIT tools in editor_plugin/Tools/*.cs can be re-registered directly
into it with no change to their internal logic — only the transport layer
(this file) would be replaced.

    MCP Client   ←── stdio ──→   mcp_server.py (this file, external)
                                        │
                                   HTTP POST 127.0.0.1:8766
                                        │
                              UNIGINE 2 Editor process
                              └── editor_plugin/ToolbeltListener.cs

Requirements:
    pip install mcp

One-time setup — add to your MCP client config (e.g. Claude Code .mcp.json):
    {
      "mcpServers": {
        "qfoldit-unigine-toolbelt": {
          "command": "python",
          "args": ["<ABSOLUTE_PATH_TO_THIS_FILE>"]
        }
      }
    }

Then load the qFoldIT Toolbelt plugin in the UNIGINE Editor (Plugins panel,
or WorldMain.cs / Editor extension autostart) so ToolbeltListener is
listening on the configured port before you connect.

Author: qFoldIT
License: AGPL-3.0, with an additional visible-attribution requirement — see LICENSE.
"""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
from typing import Any, Optional

from mcp.server.fastmcp import FastMCP

# ─── Configuration ───────────────────────────────────────────────────────

try:
    LISTENER_PORT = int(os.environ.get("UNIGINE_TOOLBELT_PORT", "8766"))
    if not (1 <= LISTENER_PORT <= 65535):
        raise ValueError(f"Port {LISTENER_PORT} out of range")
except ValueError:
    LISTENER_PORT = 8766
LISTENER_URL = f"http://127.0.0.1:{LISTENER_PORT}"
HTTP_TIMEOUT_SECONDS = 15

mcp = FastMCP("qfoldit-unigine-toolbelt")


def _post(endpoint: str, payload: dict) -> dict:
    """POST JSON to the in-editor ToolbeltListener and return the parsed JSON response."""
    url = f"{LISTENER_URL}/{endpoint}"
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=HTTP_TIMEOUT_SECONDS) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.URLError as e:
        return {
            "success": False,
            "error": f"Could not reach UNIGINE Editor at {url}: {e}. "
                     "Is the Editor open with the qFoldIT Toolbelt plugin loaded?",
        }
    except Exception as e:  # noqa: BLE001 — surfaced to the calling agent, not swallowed
        return {"success": False, "error": f"Unexpected bridge error: {e}"}


# ─── Generic dispatch (mirrors UEFN Toolbelt's run_toolbelt_tool) ────────

@mcp.tool()
def run_toolbelt_tool(tool_name: str, params_json: str = "{}") -> str:
    """Call any registered qFoldIT Unigine Toolbelt tool by name.

    Args:
        tool_name: e.g. "spawn_primitive", "arena_generate", "stamp_save".
        params_json: JSON-encoded object of parameters for that tool.
    """
    try:
        params = json.loads(params_json) if params_json else {}
    except json.JSONDecodeError as e:
        return json.dumps({"success": False, "error": f"Invalid params_json: {e}"})

    result = _post("run_tool", {"tool": tool_name, "params": params})
    return json.dumps(result)


@mcp.tool()
def list_toolbelt_tools() -> str:
    """List every qFoldIT Toolbelt tool currently registered in the running Editor,
    with category and description. Falls back to the static registry.json bundled
    with this package if the Editor listener isn't reachable."""
    result = _post("list_tools", {})
    if result.get("success"):
        return json.dumps(result)

    registry_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "registry.json")
    if os.path.exists(registry_path):
        with open(registry_path, "r", encoding="utf-8") as f:
            registry = json.load(f)
        return json.dumps({
            "success": True,
            "source": "static registry.json (Editor listener unreachable)",
            "plugins": registry.get("plugins", []),
        })
    return json.dumps(result)


@mcp.tool()
def toolbelt_get_log(max_lines: int = 100) -> str:
    """Read the last N lines of the qFoldIT Toolbelt listener log from inside the Editor."""
    result = _post("get_log", {"max_lines": max_lines})
    return json.dumps(result)


if __name__ == "__main__":
    mcp.run()
