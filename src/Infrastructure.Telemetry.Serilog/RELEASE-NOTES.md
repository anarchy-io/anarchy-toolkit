## Releases

### 2.0.0

- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.
- **Breaking:** depends on the new major versions of `Core.Telemetry`.

### 1.0.1

- chore(build): Format package release notes property

### 1.0.0

- Added optional DI integration for a consumer-configured Serilog logger, console defaults for composition roots, a
  non-DI construction path, the structured-exception and correlation-ID enrichers and opt-in log-based metrics.
