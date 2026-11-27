## Releases

### 2.0.0

- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.
- **Breaking:** depends on the new major versions of `Core.Telemetry`.

### 1.0.0

- Added an optional DogStatsD metric recorder, DI registration and telemetry module with configurable distribution metrics.
