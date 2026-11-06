# Anarchy.Toolkit.Hosting.Workers

Background-worker primitives and a self-hosted health API for worker processes.

```bash
dotnet add package Anarchy.Toolkit.Hosting.Workers
```

- Derive from `WorkerBase`, `MonitoredWorkerBase` or `ResilientWorkerBase`; `HeartbeatWorker` provides a heartbeat.
- Register the health worker with `AddServiceHealthCheckWorker(assembly)` to serve `/health/instance`, `/health/services` and `/health/status`.
- Register `IServiceHealthReport` implementations for service checks; the services endpoint returns HTTP 500 when a check fails.

See the [worker sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Worker) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
