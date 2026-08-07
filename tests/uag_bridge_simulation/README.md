# UAG Bridge simulation test

`../../editor_plugin/Tools/UAGBridgeTools.cs` (`uag_apply` / `uag_validate`)
is the adapter that connects UNIGINE-TOOLBELT to the rest of qFoldIT. It
dispatches through `ToolRegistry.Dispatch(name, JObject)` rather than
calling `Unigine.*` directly, which means its orchestration logic — node
creation order, gap collection, failure handling, connection/constraint
routing, the interaction codegen stub — can be exercised **without a live
UNIGINE Editor**, by registering fake handlers in place of the real
Unigine-backed tools and inspecting what `uag_apply` actually calls and
returns.

This directory contains that end-to-end simulation, run for real with
Mono before being committed:

- `StubToolClasses.cs` — empty `Register()` stand-ins for the 21 other
  tool category classes (SceneTools, PhysicsTools, etc.), needed only so
  `ToolRegistry.RegisterAll()` resolves outside the full plugin.
- `uag_bridge_simulation.cs` — registers fake handlers for every tool name
  `UAGBridgeTools.cs` calls, feeds it a UAG graph covering: a mapped node
  of every type, one deliberately-unmapped type (`custom`), one node that
  fails at apply time (`audio_source` with no `sound_path`), a
  `parent_child` connection, a `joint_hinge` connection, a deliberately
  unmapped connection type (`data_link`), a `physics_collision` constraint,
  a deliberately unmapped constraint type (`logic_rule`), and an
  interaction — then asserts on the exact call log and the returned
  report (23 checks).

It also verifies the "abort on invalid graph" contract: an `uag_apply`
call against a graph with a dangling `parent_id` reference returns
`success: false` and **dispatches zero tool calls** — nothing is
half-applied.

## Running

Requires `mono-mcs` (Debian/Ubuntu: `apt-get install mono-mcs mono-runtime`)
and a `Newtonsoft.Json.dll` built for `net40` or `net45` (the `netstandard2.0`
build needs a `netstandard` facade Mono may not have installed).

```bash
cd tests/uag_bridge_simulation
cp ../../editor_plugin/UagModel.cs ../../editor_plugin/UagValidator.cs ../../editor_plugin/ToolRegistry.cs .
cp ../../editor_plugin/Tools/UAGBridgeTools.cs .
mcs -langversion:latest -out:simtest.exe -r:Newtonsoft.Json.dll \
    UagModel.cs UagValidator.cs ToolRegistry.cs StubToolClasses.cs UAGBridgeTools.cs uag_bridge_simulation.cs
mono simtest.exe
```

Exits non-zero if any check fails. Note this simulates the *bridge's own
orchestration logic* — it does not (and cannot, without a UNIGINE
installation) verify that the real `spawn_primitive`/`light_create`/etc.
implementations behave as documented; see `tests/uag_validator/` for the
purely engine-agnostic validator coverage, and the `⚠` notes throughout
`editor_plugin/Tools/*.cs` for what still needs verifying against a live
SDK.
