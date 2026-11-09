# Anarchy.Toolkit.Hosting.Workers

Background-worker primitives and a self-hosted health API for worker processes.

```bash
dotnet add package Anarchy.Toolkit.Hosting.Workers
```

- Derive from `WorkerBase`, `MonitoredWorkerBase` or `ResilientWorkerBase`; `HeartbeatWorker` provides a heartbeat.
- Register the health worker with `AddServiceHealthCheckWorker(assembly)` to serve `/health/instance`, `/health/services` and `/health/status`.
- Supply an `IReadOnlyCollection<IServiceHealthReport>` for service checks; the services endpoint returns HTTP 500 when a check fails.

## When to use it

Choose this package for a long-running process that needs hosted workers, operational heartbeat or HTTP health endpoints without building a full REST API. It complements `Hosting`, which provides the application host and entry-point conventions.

## How to use it

1. Implement a worker using the base that matches the required monitoring or resilience behavior, and register it with the host's `IServiceCollection` as a hosted service. Register an `Observer` for instrumented workers.
2. If health endpoints are required, call `services.AddServiceHealthCheckWorker(typeof(YourWorker).Assembly)` from startup. Register settings implementing `IWorkerHealthCheckAppSettings`, including the health service port, and make the listener reachable only as appropriate for your deployment.
3. Supply the `IReadOnlyCollection<IServiceHealthReport>` used by the endpoint, or use a provider's health-report registration. Without reports, the package installs an empty collection. Embed build metadata in the supplied assembly if it should appear in status output.

The [sample worker startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Worker/Composition/WorkerStartup.cs) demonstrates registering a heartbeat and a custom hosted worker.

See the [worker sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Worker) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced monitored and resilient worker bases, heartbeat and health checks, a self-hosted health API and build metadata readers.
