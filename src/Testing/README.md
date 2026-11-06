# Anarchy.Toolkit.Testing

Shared test helpers, fixtures and assertions for toolkit-based applications.

```bash
dotnet add package Anarchy.Toolkit.Testing
```

- Base tests and fixtures on `TestCaseBase`, `TestCaseBase<TAppSettings>` and `EntityFixtureBase`; use `TestHelper` for common setup.
- Test instrumentation with `InMemoryObserver`, the in-memory measurer and Serilog sink, and their assertions.
- API Gateway response assertions use AwesomeAssertions. Database, AWS messaging and web-specific helpers live in separate `Testing.*` packages.

See the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and [package catalogue](https://github.com/anarchy-io/anarchy-toolkit#package-catalogue).
