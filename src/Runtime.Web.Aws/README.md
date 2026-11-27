# Anarchy.Toolkit.Runtime.Web.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

AWS Lambda hosting for toolkit REST APIs behind API Gateway.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Web.Aws
```

- Derive the Lambda handler from `AwsWebAppEntryPointBase<TStartup>`; it adds the `X-Amzn-Trace-Id` middleware and registers `IAmazonRequestContext`.
- Other hosts behind AWS opt into the trace id middleware with `app.UseAmazonTraceId()`.
- `AspNetCoreServerHelper.StripLeadingPathPrefix` removes an API Gateway stage prefix from request paths.

## When to use it

Use this package for APIs deployed as AWS Lambda functions. Native and container hosts only need `Anarchy.Toolkit.Runtime.Web`.

## How to use it

```csharp
public sealed class EntryPointLambda : AwsWebAppEntryPointBase<Startup>;
```

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
