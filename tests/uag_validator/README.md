# UAG Validator standalone tests

`../../editor_plugin/UagValidator.cs` (the file actually shipped in the
plugin) has zero dependency on `Unigine.*` types — it only operates on the
plain C# model classes in `../../editor_plugin/UagModel.cs`. That means its
core logic (dangling-reference checks, `parent_child` cycle detection, gap
reporting) can be compiled and run **outside** the UNIGINE Editor, with a
plain C# compiler — useful since there's no UNIGINE SDK available in most
CI environments to run a true in-Editor integration test against.

This directory contains that standalone verification:

- `UagModelForTest.cs` — a Newtonsoft-free copy of the UAG model (same
  field names/shape as the real `UagModel.cs`), used only so this test
  doesn't need the Newtonsoft.Json.dll the real plugin depends on.
- `uag_validator_tests.cs` — 10 scenarios / 24 assertions: valid graphs,
  dangling `parent_id`/`from_node`/`to_node`/`target_node` references,
  1-node/2-node/3-node cycles, a long non-cyclic chain (checking for
  false positives), duplicate ids, and gap reporting (unmapped types are
  informational, not validation errors).

These were run for real (not just written) with Mono before being
committed — 24/24 assertions passed for both the Unity and UNIGINE
variants of `UagValidator.cs` (the algorithm is identical between the two;
only the per-engine `MappedNodeTypes`/`MappedConstraintTypes` sets differ).

## Running

Requires `mono-mcs` (Debian/Ubuntu: `apt-get install mono-mcs mono-runtime`).

```bash
cd tests/uag_validator
cp ../../editor_plugin/UagValidator.cs .
mcs -out:uagtest.exe UagModelForTest.cs UagValidator.cs uag_validator_tests.cs
mono uagtest.exe
```

Exits non-zero if any assertion fails.

Note: `UagValidator.cs` itself is not duplicated in this directory — copy
the real one from `editor_plugin/` before running, so the test always
exercises the exact file that ships in the plugin, not a stale copy.
