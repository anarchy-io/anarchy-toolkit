# Anarchy.Toolkit.Testing.Runtime.Web

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Test helpers for REST APIs using the toolkit's web pipeline.

```bash
dotnet add package Anarchy.Toolkit.Testing.Runtime.Web
```

- Derive from `RestApiTestCaseBase<TStartup, TAppSettings>` to exercise the native request pipeline; `AwsRestApiTestCaseBase` in `Anarchy.Toolkit.Testing.Runtime.Web.Aws` adds the AWS Lambda pipeline.
- `MockAuthorizationToken<TAppSettings>(scopes)` helps with authorization scenarios.
- The test bases accept any `IAppSettings`; they add the mock bearer token only when the settings implement `IAuthServiceAppSettings`, so APIs without authentication can use them too.
- Use the shared [Testing](https://www.nuget.org/packages/Anarchy.Toolkit.Testing) package for fixtures and assertions.

## When to use it

Use this package for API tests that need to exercise routing, middleware, authentication and response serialization through the toolkit's native ASP.NET Core pipeline (and, with `Testing.Runtime.Web.Aws`, the Lambda one). It complements unit tests; external dependencies still need appropriate test implementations or isolated infrastructure.

## How to use it

1. Derive a test class from `RestApiTestCaseBase<TStartup, TAppSettings>`, supplying a `WebApplicationFactory<TStartup>` and the application's authorization scopes. The settings type must implement `IAuthServiceAppSettings`.
2. Build requests with `Get`, `Post`, `Put` or `Patch`, and assert status and parsed output with the endpoint test methods. Configure embedded test settings and the mock token issuer for authenticated scenarios.
3. Use `TestEndpointAsync` (or `TestEndpointNativeAsync`) to send a request and assert its status and output. With `AwsRestApiTestCaseBase`, `TestEndpointAsync` also runs the Lambda pipeline and compares both responses.

The runnable [greeting API test](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Testing/Api/GreetingControllerTest.cs) shows the fixture constructor and a request assertion.

See the [sample tests](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Testing), [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
