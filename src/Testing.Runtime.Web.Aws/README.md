# Anarchy.Toolkit.Testing.Runtime.Web.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Test helpers for REST APIs deployed both natively and as AWS Lambda functions.

```bash
dotnet add package Anarchy.Toolkit.Testing.Runtime.Web.Aws
```

- Derive from `AwsRestApiTestCaseBase<TStartup, TAppSettings>`: `TestEndpointAsync` runs the native and Lambda pipelines and compares them; `TestEndpointLambdaAsync` runs the Lambda pipeline only.
- `TestableLambdaEntrypoint` hosts the Lambda pipeline in the Development environment.
- `ToApiGatewayProxyRequestAsync` converts an `HttpRequestMessage`; `response.Should().HaveStatusCodeOrDump(...)` asserts API Gateway responses.

## When to use it

Use this package for APIs that ship an `AwsWebAppEntryPointBase` handler. APIs without a Lambda entry point use `Anarchy.Toolkit.Testing.Runtime.Web`.

## How to use it

```csharp
public class OrdersApiTest(WebApplicationFactory<Startup> factory)
    : AwsRestApiTestCaseBase<Startup, AppSettings>(new TestableLambdaEntrypoint<Startup>(), factory, ["orders:read"]);
```

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
