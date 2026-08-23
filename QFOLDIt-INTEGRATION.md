# qFoldIT Runtime Adapter

UNIGINE-TOOLBELT is an engine-specific runtime adapter.

Canonical qFoldIT semantics live in `UEFN-QFOLDIT/crates/qfoldit-core` and are transported through the UWI/Scientific Action Envelope boundary.

```text
MissionContract
   ↓
Scientific State
   ↓
UWI / UNIGINE adapter
   ↓
UNIGINE Toolbelt
   ↓
UNIGINE runtime
```

Keep engine-specific implementation here; do not create a competing mission/state/provenance authority.
