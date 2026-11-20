# Anarchy.Toolkit.Infrastructure.CorrelationId

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Provider-neutral correlation and attempt ID propagation for ASP.NET Core services, workers and message consumers.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.CorrelationId
```

- Access IDs through the async-flowing `CorrelationContext`.
- Add `UseCorrelationId()` to the ASP.NET Core pipeline; headers default to `X-Correlation-Id` and `X-Attempt-Id` and are configurable.
- Correlation and attempt IDs are added to standard `ILogger` scopes and the current `Activity` as tags, so logging and tracing providers can consume them without a Serilog dependency.
- Use `CorrelationContext.BeginScope()` in workers and message consumers to set IDs and open the same provider-neutral scope for an operation.

## When to use it

Use correlation and attempt IDs to connect log events and traces from the same request or retry across service boundaries. The package provides inbound HTTP middleware and a helper for non-HTTP entry points; it does not automatically configure every outgoing HTTP client or message publisher to forward IDs.

## How to use it

1. Register `ModuleCorrelationId` with the application's configuration and environment. Pass a `CorrelationIdOptions` factory to customize header names, log property names or missing-header behavior.
2. Add `UseCorrelationId()` early enough in the ASP.NET Core pipeline for later middleware and endpoints to see the context.
3. Use `CorrelationContext.BeginScope(logger, correlationId, attemptId)` around worker or message-consumer work. It sets the async-flowing context, opens a standard logging scope and adds tags to the current activity.

The middleware and helper use both logging scopes and activity tags: scopes make the values available to any `ILogger` provider; tags make them available to tracing and OpenTelemetry pipelines. They do not use baggage, which would implicitly propagate values to downstream services. Use the optional `Anarchy.Toolkit.Infrastructure.Telemetry.Serilog` integration only when Serilog-specific event enrichment is needed.

The default behavior generates missing values. `IgnoreIfMissing` and `ThrowIfMissing` are available when the application instead wants to accept missing headers or reject them. Coordinate header names with upstream and downstream services.

See the [observability guide](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.0.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced correlation ID context and options, HTTP header propagation middleware, Serilog enrichment and dependency-injection wiring.
