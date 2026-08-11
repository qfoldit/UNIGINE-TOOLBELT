# UAG Bridge simulation test

`../../editor_plugin/Tools/UAGBridgeTools.cs` (`uag_apply` / `uag_validate`)
is the adapter that connects UNIGINE-TOOLBELT to the rest of qFoldIT, now
conforming to `qfoldit-engine-adapter-spec-v0.1`'s formal schema (see
`../../docs/UAG_BRIDGE.md`). It dispatches through
`ToolRegistry.Dispatch(name, JObject)` rather than calling `Unigine.*`
directly, which means its orchestration logic — node creation dispatch,
gap collection, failure handling, hierarchy/constraint/interaction/binding
routing — can be exercised **without a live UNIGINE Editor**, by
registering fake handlers in place of the real Unigine-backed tools and
inspecting what `uag_apply` actually calls and returns.

This directory contains that end-to-end simulation, run for real with
Mono before being committed (updated in place for the new schema — this
is the second revision of this harness, not a new one):

- `StubToolClasses.cs` — empty `Register()` stand-ins for the 23 other
  tool category classes, needed only so `ToolRegistry.RegisterAll()`
  resolves outside the full plugin.
- `uag_bridge_simulation.cs` — registers fake handlers for every tool name
  `UAGBridgeTools.cs` calls, feeds it a graph covering: a
  `scientific_subject/construction` node with a matching `construction`
  interaction and a binding (the exact shape
  `qfoldit-scientific-gameplay-framework-v0.1`'s `reference/compiler.py`
  emits), a legacy `molecular_structure`/`interaction_zone` pair (the
  spec's own hand-authored example shape), an `audio_source` missing its
  required `sound_path` (must fail cleanly with **no orphan node** — the
  Phase 2 bug this repo already fixed once, re-verified here against the
  new schema), a deliberately-unmapped node type, a `physics.joint`
  constraint, and a deliberately-unmapped constraint type — 22 assertions
  checking both the returned execution report **and** the exact sequence
  of tool names/parameters dispatched.

It also verifies the abort contract directly: an `uag_apply` call against
a graph with the wrong `schema` value returns `status: "failed"` with the
`INVALID_SCHEMA` error code and **dispatches zero tool calls**.

## Running

Requires `mono-mcs` and a `net40`/`net45` `Newtonsoft.Json.dll`.

```bash
cd tests/uag_bridge_simulation
cp ../../editor_plugin/UagModel.cs ../../editor_plugin/UagValidator.cs ../../editor_plugin/UAGBridgeMechanics.cs ../../editor_plugin/ToolRegistry.cs .
cp ../../editor_plugin/Tools/UAGBridgeTools.cs .
mcs -langversion:latest -out:simtest.exe -r:Newtonsoft.Json.dll \
    UagModel.cs UagValidator.cs UAGBridgeMechanics.cs ToolRegistry.cs StubToolClasses.cs UAGBridgeTools.cs uag_bridge_simulation.cs
mono simtest.exe
```

Exits non-zero if any check fails. This simulates the *bridge's own
orchestration logic* — it does not (and cannot, without a UNIGINE
installation) verify that the real `spawn_primitive`/`light_create`/
`interaction_create`/`scientific_visualization_create`/etc.
implementations behave as documented; see the `⚠` notes throughout
`editor_plugin/Tools/*.cs` for what still needs verifying against a live
SDK, and each new tool file's header for its own honest scope notes
(`InteractionTools.cs` and `ScientificVisualizationTools.cs` in
particular — both explicitly document what they do *not* attempt, unlike
guessing at unverified UNIGINE Input/3D-text APIs).
