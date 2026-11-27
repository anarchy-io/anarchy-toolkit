# Anarchy.Toolkit.Runtime.Workers

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Background-worker primitives and a self-hosted health API for worker processes.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Workers
```

- Derive from `WorkerBase`, `MonitoredWorkerBase` or `ResilientWorkerBase`; `HeartbeatWorker` provides a heartbeat.
- `ResilientWorkerBase` gives every iteration a new correlation id and every attempt a new attempt id, through `CorrelationContext.BeginScope`: they reach every logging provider as scope properties and the current activity as tags. Work that carries its own ids (a consumed message) opens `BeginCorrelationScope(correlationId, attemptId)` around its processing.
- Register the health worker with `AddServiceHealthCheckWorker(assembly)` to serve `/health/instance`, `/health/services` and `/health/status`.
- Supply an `IReadOnlyCollection<IHealthCheck>` for service checks; the services endpoint returns HTTP 500 when a check fails.

## When to use it

Choose this package for a long-running process that needs hosted workers, operational heartbeat or HTTP health endpoints without building a full REST API. It complements `Runtime`, which provides the application host and entry-point conventions.

## How to use it

1. Implement a worker using the base that matches the required monitoring or resilience behavior, and register it with the host's `IServiceCollection` as a hosted service. Instrumented workers take the registered `AppTelemetry` (see `ModuleTelemetry` or `AddCoreTelemetry()`).
2. If health endpoints are required, call `services.AddServiceHealthCheckWorker(typeof(YourWorker).Assembly)` from startup. Register settings implementing `IWorkerHealthCheckAppSettings`, including the health service port, and make the listener reachable only as appropriate for your deployment.
3. Supply the `IReadOnlyCollection<IHealthCheck>` used by the endpoint, or use a provider's health-report registration. Without reports, the package installs an empty collection. Embed build metadata in the supplied assembly if it should appear in status output.

The [sample worker startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Worker/Composition/SampleWorkerStartup.cs) demonstrates registering a heartbeat and a custom hosted worker.

See the [worker sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Worker) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Microsoft.Extensions.Hosting.Systemd](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Systemd) | The self-hosted health API enables systemd integration | No |
| [Polly](https://www.nuget.org/packages/Polly.Core) (`Polly.Core`) | Retries `ResilientWorkerBase` iterations | No |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
