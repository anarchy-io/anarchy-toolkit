# Anarchy.Toolkit.Testing.Web

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Test helpers for REST APIs using the toolkit's web pipeline.

```bash
dotnet add package Anarchy.Toolkit.Testing.Web
```

- Derive from `RestApiTestCaseBase<TStartup, TAppSettings>` to exercise native and AWS Lambda request pipelines.
- `TestableLambdaEntrypoint` supports Lambda-based tests; `MockAuthorizationToken<TAppSettings>(scopes)` helps with authorization scenarios.
- Use the shared [Testing](https://www.nuget.org/packages/Anarchy.Toolkit.Testing) package for fixtures and assertions.

## When to use it

Use this package for API tests that need to exercise routing, middleware, authentication and response serialization through the toolkit's native ASP.NET Core and Lambda hosting paths. It complements unit tests; external dependencies still need appropriate test implementations or isolated infrastructure.

## How to use it

1. Derive a test class from `RestApiTestCaseBase<TStartup, TAppSettings>`, supplying a `WebApplicationFactory<TStartup>`, a `TestableLambdaEntrypoint<TStartup>` and the application's authorization scopes. The settings type must implement `IAuthServiceAppSettings`.
2. Build requests with `Get`, `Post`, `Put` or `Patch`, and assert status and parsed output with the endpoint test methods. Configure embedded test settings and the mock token issuer for authenticated scenarios.
3. Use `TestEndpointAsync` when native and Lambda responses should be deterministic and equal. For timestamps, generated IDs or other variable output, call `TestEndpointNativeAsync` and `TestEndpointLambdaAsync` separately and assert the relevant behavior in each.

The runnable [greeting API test](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Tests/Api/GreetingControllerTests.cs) shows the fixture constructor and a request assertion.

See the [sample tests](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Tests), [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

### 1.2.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1
- feat(Web): serve OpenAPI 3.2 and theme Swagger UI with a light and dark Anarchy theme

### 1.0.8

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.7

- References `Anarchy.Toolkit.Web` 1.0.7, aligned with the .NET 10 LTS.

### 1.0.6

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

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

- Introduced REST API test bases, a testable Lambda entry point and HTTP request helpers.
