# Conformance tests — qfoldit-engine-adapter-spec-v0.1

> This directory supersedes and replaces the earlier `tests/uag_validator/`
> (Phase 1), which used a hand-maintained stand-in copy of the UAG model
> against ad-hoc scenarios. This directory tests the **real, unmodified**
> `UagModel.cs` (with actual Newtonsoft.Json deserialization, not a
> stripped stand-in) against the spec's **own official** test vectors —
> strictly more rigorous, so the older directory was removed rather than
> updated in parallel.

Mirrors `qfoldit-unity-toolbelt/tests/conformance/` exactly — verifies
`../../editor_plugin/UagModel.cs` and `../../editor_plugin/UagValidator.cs`
against the **real, unmodified** artifacts from
`qfoldit-engine-adapter-spec-v0.1`:

- `test_vectors.json` — copied verbatim from the spec package.
- `protein-folding.uag.json` — the spec's own hand-authored example.
- `compiler_output_unigine.json` — the **actual output** of running
  `qfoldit-scientific-gameplay-framework-v0.1`'s real `compile_pattern()`
  against the real `protein_folding_construction` pattern and this
  engine's manifest, captured once and committed so the parse test
  doesn't depend on having Python available.

## Running

Requires `mono-mcs` and a `net40`/`net45` `Newtonsoft.Json.dll`.

```bash
cd tests/conformance
cp ../../editor_plugin/UagModel.cs ../../editor_plugin/UagValidator.cs ../../editor_plugin/UAGBridgeMechanics.cs .
mcs -langversion:latest -out:conformance.exe -r:Newtonsoft.Json.dll UagModel.cs UagValidator.cs UAGBridgeMechanics.cs conformance_test.cs
mcs -langversion:latest -out:parsetest.exe -r:Newtonsoft.Json.dll UagModel.cs uag_schema_parse_test.cs
mono conformance.exe
mono parsetest.exe
```

Both exit non-zero if any check fails. `tests/uag_validator/` and
`tests/uag_bridge_simulation/` are this repo's earlier Phase 1/2
verification harnesses — they target the pre-this-revision UAG shape
(`parent_id`, `connections[]`) and need updating to the current
`UagModel.cs`/`UagValidator.cs`/`UAGBridgeTools.cs` before they'll compile
again; this directory (`tests/conformance/`) is the current, up-to-date
verification and should be treated as authoritative until those two are
brought forward.
