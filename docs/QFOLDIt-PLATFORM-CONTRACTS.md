# qFoldIT Platform Contract Alignment

## Role

UNIGINE-TOOLBELT is the industrial and engineering runtime adapter within the qFoldIT platform. It is optimized for simulation-oriented world execution and visualization.

## Canonical contracts

- `qfoldit.mission/1.0`
- `qfoldit.scientific-state/1.0`
- `qfoldit.uag/1.0`
- `qfoldit.engine-adapter/1.0`
- `qfoldit.event/1.0`

## Adapter principle

Scientific missions enter the runtime through UAG. UNIGINE-specific implementation details remain inside the adapter layer. Scientific validation and commercial policy remain external authorities.

## Capability declaration

The adapter manifest is the authoritative capability declaration and should be checked by conformance tests before a capability is advertised as production-ready.

## Runtime flow

```text
Mission
  -> UAG
  -> UNIGINE adapter
  -> industrial simulation/world
  -> evidence/state projection
  -> mission orchestration
  -> scientific validation
```
