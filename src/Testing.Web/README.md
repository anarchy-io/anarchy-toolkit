# Anarchy.Toolkit.Testing.Web

Test helpers for REST APIs using the toolkit's web pipeline.

```bash
dotnet add package Anarchy.Toolkit.Testing.Web
```

- Derive from `RestApiTestCaseBase<TStartup, TAppSettings>` to exercise native and AWS Lambda request pipelines.
- `TestableLambdaEntrypoint` supports Lambda-based tests; `MockAuthorizationToken<TAppSettings>(scopes)` helps with authorization scenarios.
- Use the shared [Testing](https://www.nuget.org/packages/Anarchy.Toolkit.Testing) package for fixtures and assertions.

See the [sample tests](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Tests), [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
