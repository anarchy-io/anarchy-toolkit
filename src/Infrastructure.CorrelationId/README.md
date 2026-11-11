# Anarchy.Toolkit.Infrastructure.CorrelationId

Correlation and attempt ID propagation for ASP.NET Core services and Serilog.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.CorrelationId
```

- Access IDs through the async-local `CorrelationContext`.
- Add `UseCorrelationId()` to the ASP.NET Core pipeline; headers default to `X-Correlation-Id` and `X-Attempt-Id` and are configurable.
- Add the Serilog `WithCorrelationId()` enricher and register services through `ModuleCorrelationId`.

## When to use it

Use correlation and attempt IDs to connect log events from the same request or retry across service boundaries. The package provides inbound HTTP middleware and log enrichment; it does not automatically configure every outgoing HTTP client or message publisher to forward headers.

## How to use it

1. Register `ModuleCorrelationId` with the application's configuration and environment. Pass a `CorrelationIdOptions` factory to customize header names or missing-header behavior.
2. Add `UseCorrelationId()` early enough in the ASP.NET Core pipeline for later middleware and endpoints to see the context.
3. Add the Serilog `WithCorrelationId()` enricher to the logger and propagate the IDs to downstream requests where required. `CorrelationContext` stores the current values in async-local context.

The default behavior generates missing values. `IgnoreIfMissing` and `ThrowIfMissing` are available when the application instead wants to accept missing headers or reject them. Coordinate header names with upstream and downstream services.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced correlation ID context and options, HTTP header propagation middleware, Serilog enrichment and dependency-injection wiring.
