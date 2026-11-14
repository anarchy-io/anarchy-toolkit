# Anarchy.Toolkit.Testing

Shared test helpers, fixtures and assertions for toolkit-based applications.

```bash
dotnet add package Anarchy.Toolkit.Testing
```

- Base tests and fixtures on `TestCaseBase`, `TestCaseBase<TAppSettings>` and `EntityFixtureBase`; use `TestHelper` for common setup.
- Test instrumentation with `InMemoryObserver`, the in-memory measurer and Serilog sink, and their assertions.
- API Gateway response assertions use AwesomeAssertions. Database, AWS messaging and web-specific helpers live in separate `Testing.*` packages.

## When to use it

Reference this package from test projects, not production services. It is useful for focused unit tests that need repeatable entities, a host environment or an observer without sending logs and metrics to a real backend. Use the specialized test kits when the test needs a database, messaging service or web host.

## How to use it

1. Derive a test class from `TestCaseBase` and pass its `FixtureObserver` to the subject under test. Assert business behavior first; inspect `SinkLogs` or `InMemoryMeasurer` when telemetry itself is under test.
2. Derive fixtures from `EntityFixtureBase<T>` and implement `BuildDefaultFixture()`. Call `Build()` for defaults or `Build(new { Property = value })` to vary only the scenario-specific fields. Null overrides require `overrideWithNulls: true`.
3. Use `TestCaseBase<TAppSettings>` or `TestHelper.TestSettings<TAppSettings>()` for typed settings. Embed `appsettings.json` and `appsettings.Development.json` in the assembly declaring the settings type, and keep real credentials out of test resources.

See the [testing guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) for a test and fixture example.

See the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and [package catalogue](https://github.com/anarchy-io/anarchy-toolkit#package-catalogue).

## Releases

### 1.0.6

- refactor(src): clean up code style across library packages

### 1.0.5

- chore(deps): update AWS SDK packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced shared test bases, fixtures, request and response helpers, and in-memory observability assertions.
