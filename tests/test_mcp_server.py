"""
qFoldIT Toolbelt for UNIGINE 2 — tests for mcp_server.py
=========================================================
These tests exercise the bridge's own logic (JSON handling, error
messages, fallback to the static registry.json) WITHOUT needing a running
UNIGINE Editor. They deliberately do not mock urllib beyond pointing at a
closed port, so the "Editor unreachable" path is exercised for real.

Run:
    pip install -r ../requirements.txt
    python -m pytest test_mcp_server.py -v
"""

import json
import os
import sys

import pytest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import mcp_server  # noqa: E402


def test_default_port_is_8766():
    assert mcp_server.LISTENER_PORT == 8766 or "UNIGINE_TOOLBELT_PORT" in os.environ


def test_run_toolbelt_tool_rejects_invalid_json():
    result_json = mcp_server.run_toolbelt_tool("spawn_primitive", params_json="{not valid json")
    result = json.loads(result_json)
    assert result["success"] is False
    assert "Invalid params_json" in result["error"]


def test_run_toolbelt_tool_reports_unreachable_editor():
    # No Editor is running during CI, so this should hit the URLError branch
    # in _post() and return a clear, actionable error rather than raising.
    result_json = mcp_server.run_toolbelt_tool("spawn_primitive", params_json="{}")
    result = json.loads(result_json)
    assert result["success"] is False
    assert "Could not reach UNIGINE Editor" in result["error"]


def test_list_toolbelt_tools_falls_back_to_static_registry():
    result_json = mcp_server.list_toolbelt_tools()
    result = json.loads(result_json)
    assert result["success"] is True
    assert "plugins" in result
    assert len(result["plugins"]) == 22  # 22 category files as of this release


def test_registry_json_tool_count_matches_source_annotations():
    registry_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "registry.json")
    with open(registry_path, "r", encoding="utf-8") as f:
        registry = json.load(f)

    total_declared = sum(p["tool_count"] for p in registry["plugins"])
    total_listed = sum(len(p["tools"]) for p in registry["plugins"])
    assert total_declared == total_listed == 80


def test_post_handles_generic_exception_gracefully(monkeypatch):
    def boom(*args, **kwargs):
        raise RuntimeError("simulated failure")

    monkeypatch.setattr(mcp_server.urllib.request, "urlopen", boom)
    result = mcp_server._post("run_tool", {"tool": "x", "params": {}})
    assert result["success"] is False
    assert "Unexpected bridge error" in result["error"]


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-v"]))
