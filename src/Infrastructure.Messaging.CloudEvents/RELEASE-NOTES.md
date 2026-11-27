## Releases

### 2.0.0

- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.
- **Breaking:** depends on the new major versions of `Core` and `Core.Events`.

### 1.0.0

- Introduce injectable, per-property customizable CloudEvents 1.0 mapping for event envelopes and broker-neutral messages, with structured JSON and custom attributes.
