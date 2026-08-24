# qFoldIT Runtime Adapter Boundary

This repository is a runtime-specific capability source for the canonical qFoldIT Rust runtime-adapter contract.

Canonical contract: `qfoldit.runtime-adapter/1.0`

Engine-specific capabilities remain here. Scientific mission/state/provenance/authorization semantics remain canonical in `qfoldit/UEFN-QFOLDIT`.

```text
qfoldit-core
  -> runtime-adapters
  -> UNIGINE adapter
  -> UNIGINE runtime
  -> Submission
  -> Validator
  -> Evidence 1.1
```

New cross-engine runtime semantics belong in `qfoldit/UEFN-QFOLDIT/crates/runtime-adapters`.
