# Anarchy.Toolkit.Testing

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Shared test helpers, fixtures and assertions for toolkit-based applications.

```bash
dotnet add package Anarchy.Toolkit.Testing
```

- Base tests and fixtures on `TestCaseBase`, `TestCaseBase<TAppSettings>` and `EntityFixtureBase`; use `TestHelper` for common setup.
- Test instrumentation with `TestTelemetry`: an `AppTelemetry` whose logs (`InMemoryLoggerProvider`, with properties and scopes) and metrics (`InMemoryMetricRecorder`) are captured in memory. `services.AddTestTelemetry(capture)` routes a container's telemetry to the same capture.
- Database, AWS and web-specific helpers (including API Gateway response assertions) live in separate `Testing.*` packages.

## When to use it

Reference this package from test projects, not production services. It is useful for focused unit tests that need repeatable entities, a host environment or telemetry without sending logs and metrics to a real backend. Use the specialized test kits when the test needs a database, messaging service or web host.

## How to use it

1. Derive a test class from `TestCaseBase` and pass `FixtureTelemetry.Telemetry` to the subject under test. Assert business behavior first; inspect `FixtureTelemetry.Logs` or `FixtureTelemetry.Metrics` when telemetry itself is under test.
2. Derive fixtures from `EntityFixtureBase<T>` and implement `BuildDefaultFixture()`. Call `Build()` for defaults or `Build(new { Property = value })` to vary only the scenario-specific fields. Null overrides require `overrideWithNulls: true`.
3. Use `TestCaseBase<TAppSettings>` or `TestHelper.TestSettings<TAppSettings>()` for typed settings. Embed `appsettings.json` and `appsettings.Development.json` in the assembly declaring the settings type, and keep real credentials out of test resources.

See the [testing guide](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) for a test and fixture example.

See the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [xUnit.net v3](https://www.nuget.org/packages/xunit.v3.extensibility.core) (`xunit.v3.extensibility.core`) | The test framework the kits are written for; `Xunit` is a global using | No: brought to test projects |
| [Moq](https://www.nuget.org/packages/Moq) | Mocks for the test projects | No: brought to test projects |
| [AwesomeAssertions](https://www.nuget.org/packages/AwesomeAssertions) | Fluent assertions, extended by the kit's assertion extensions | Yes: the assertion extensions build on it |
| [Spectre.Console](https://www.nuget.org/packages/Spectre.Console) | Console output for test projects | No: brought to test projects |
| [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) | Parses response bodies (`ResponseHelper`) | No |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
