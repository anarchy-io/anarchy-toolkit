# Observability

This guide describes how applications built on the toolkit write logs and emit metrics through `ICoreTelemetry` and `AppTelemetry`, how correlation IDs reach every log line, and how to send it all to the backend your organization uses.

> **A guide, not a rule.** This page explains how the platform works. The conventions it mentions are recommendations: see the [Observability convention](../conventions/observability.md) for the rules, why they exist and how to opt out.
{: .notice--info}

The toolkit adds **no logging or metrics backend of its own**. It writes through the standard Microsoft logging ([`ILogger`](https://learn.microsoft.com/dotnet/core/extensions/logging)) and .NET metrics ([`System.Diagnostics.Metrics`](https://learn.microsoft.com/dotnet/core/diagnostics/metrics)) APIs, so whatever your application plugs into them (the console, [OpenTelemetry](https://opentelemetry.io/docs/languages/dotnet/) and its OTLP exporter, [Serilog](https://serilog.net/), a vendor's logging provider) receives the toolkit's telemetry too, without changing any instrumentation code.

## The building blocks

| Package | What it provides |
|---|---|
| `Anarchy.Toolkit.Core.Telemetry` | `ICoreTelemetry`: the primitives (`ILogger Logger`, `IMetricRecorder Metrics`). `AppTelemetry`: the toolkit conventions on top of them (call sites, standard metric names, timers, monitored actions, log helpers, `LogContext` scopes). `AddCoreTelemetry()` and `ModuleTelemetryBase` for registration. |
| `Anarchy.Toolkit.Infrastructure.CorrelationId` | `CorrelationContext`, the `UseCorrelationId()` middleware and `CorrelationContext.BeginScope(...)` for non-HTTP entry points. Provider-neutral: no Serilog dependency. |
| `Anarchy.Toolkit.Configuration` | `ModuleTelemetry`, which registers telemetry, the correlation context and `IApplicationContext`; `ModuleLibConfiguration` applies it with the settings. |
| `Anarchy.Toolkit.Infrastructure.Telemetry.Serilog` | Optional. Serilog as one more logging provider, a console default for tools, a correlation enricher, a structured-exception enricher and opt-in log-based metrics. |
| `Anarchy.Toolkit.Infrastructure.Messaging.Aws` | `InboundMessage.BeginCorrelationScope(logger)`: the entry point for consumers of mapped SQS messages. |
| `Anarchy.Toolkit.Testing` | `TestTelemetry`: an `AppTelemetry` whose logs and metrics are captured in memory, for tests. |

Which of these to install is summarized in [Which package do I need?](../packages/README.md#which-package-do-i-need).

## Start here

Install `Anarchy.Toolkit.Core.Telemetry` (the host packages `Configuration`, `Web` and `Hosting.Workers` already bring it), register it once and inject `AppTelemetry`:

```csharp
services.AddCoreTelemetry();
```

```csharp
public sealed class OrderService(AppTelemetry telemetry)
{
    public Task ProcessAsync(Order order, CancellationToken cancellationToken) =>
        telemetry.MonitoredActionAsync(this.CalledFrom(), async () =>
        {
            telemetry.LogInformation(this.CalledFrom(), "Processing order {OrderId}", order.Id);
            await repository.SaveAsync(order, cancellationToken);
        }, cancellationToken);
}
```

Applications that use `ModuleLibConfiguration` (every scaffolded host) get this registration, plus the correlation context, from `ModuleTelemetry`; they do not call `AddCoreTelemetry()` themselves.

Toolkit packages (database policies, HTTP repositories, workers, the event pipeline, the web middleware) take the same registered `AppTelemetry`, so their logs and metrics land in the same pipeline as yours.

## Call sites

`AppTelemetry` attaches the calling class and method to every log line and metric, so call sites carry the context:

```csharp
var caller = this.CalledFrom();

telemetry.LogInformation(caller, "Processing user {UserId}", userId);
telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, new("success", true));
```

If `this.CalledFrom()` is not available (for example in a static method), build the call site explicitly:

```csharp
var caller = new CallSite("UserMapper", "Map");
telemetry.LogWarning(caller, "Falling back to default role for {UserId}", userId);
```

The log line is prefixed `[UserMapper → Map]`; the metric is named `user_mapper.map.<suffix>`. How to word messages, name metrics and choose tags is in the [Observability convention](../conventions/observability.md).

## Where the telemetry goes

| Signal | API | Reaches |
|---|---|---|
| Logs | `ICoreTelemetry.Logger`, an `ILogger` created from the application's `ILoggerFactory` | Every logging provider registered with `AddLogging(...)` |
| Metrics | `IMetricRecorder`, by default recording on the `Anarchy.Toolkit.Telemetry` meter of the application's `IMeterFactory` | Anything that listens to that meter, such as OpenTelemetry |
| Traces | The current `Activity` (ASP.NET Core and your own instrumentation), tagged with the correlation IDs | Whatever exports activities |

The toolkit's logger category and meter share one name, exposed as `CoreTelemetry.InstrumentationName` (`Anarchy.Toolkit.Telemetry`). Durations are recorded in seconds.

### Registration, precedence and ownership

`AddCoreTelemetry()` registers `IMetricRecorder`, `ICoreTelemetry` and `AppTelemetry` with `TryAdd`, then calls `AddLogging()` and `AddMetrics()`, which only fill in what is missing:

- **Precedence.** A registration made before `AddCoreTelemetry()` is kept. One made after it wins, because the container resolves the last registration; `services.Replace(...)` makes that explicit. Registering your own `IMetricRecorder`, `ICoreTelemetry` or even `AppTelemetry` is supported, in any order.
- **Providers.** The default adds no sink. Logs go wherever the application's logging providers send them.
- **Ownership.** The toolkit types never dispose what they are given. Instances you register stay yours; the default logger and meter belong to their factories, which the container disposes.

Console tools and tests that do not use DI build the same thing directly: `AppTelemetry.Create(loggerFactory, meterFactory)`. `AppTelemetry.Null` discards everything.

## Sending telemetry to a backend

### OpenTelemetry and OTLP (preferred)

[OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/) collects the toolkit's meter and exports it with the [OTLP](https://opentelemetry.io/docs/specs/otlp/) protocol, which Datadog, New Relic, Grafana, the OpenTelemetry Collector and most other backends accept. With the `OpenTelemetry.Extensions.Hosting` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` packages:

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders-api"))
    .WithMetrics(metrics => metrics
        .AddMeter(CoreTelemetry.InstrumentationName)
        .AddOtlpExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()          // OpenTelemetry.Instrumentation.AspNetCore
        .AddOtlpExporter())
    .WithLogging(logging => logging.AddOtlpExporter());

services.Configure<OpenTelemetryLoggerOptions>(options => options.IncludeScopes = true); // correlation IDs
services.AddCoreTelemetry();
```

No toolkit-specific exporter or recorder is involved: the default `MeterMetricRecorder` creates each instrument once and OpenTelemetry reads it. `IncludeScopes` is what carries the [correlation scope](#correlation-ids) into exported logs.

The exporter reads the standard `OTEL_EXPORTER_OTLP_*` environment variables, so the destination is configuration, not code:

| Variable | Purpose |
|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Where to send telemetry |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` or `http/protobuf` |
| `OTEL_EXPORTER_OTLP_HEADERS` | Authentication, as `key=value` pairs |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | `cumulative` (default), `delta` or `lowmemory` |
| `OTEL_SERVICE_NAME` | The service name |

### Datadog and New Relic

Both vendors ingest OTLP, so the setup is configuration only: no vendor package and no vendor-specific API in your code. The toolkit's own test exports metrics to a local OTLP HTTP receiver; it does **not** verify delivery to either vendor, so check the result in your account.

| | Datadog | New Relic |
|---|---|---|
| Destination | The [Datadog Agent with OTLP ingestion enabled](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest_in_the_agent/) (usually `http://localhost:4318` or `:4317`), or the [direct metrics endpoint](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/metrics/) | The [New Relic OTLP endpoint](https://docs.newrelic.com/docs/opentelemetry/best-practices/opentelemetry-otlp/) for your region |
| Authentication | The Agent holds the API key; the direct endpoint takes it in a header | Your license key in an `api-key` header |
| Protocol | The Agent accepts gRPC and HTTP; the direct endpoint requires HTTP | `http/protobuf` or gRPC |
| Temporality | Direct endpoint requires `delta` | `delta` recommended |

```bash
# Example for New Relic: take the endpoint and header from your account's documentation.
export OTEL_EXPORTER_OTLP_ENDPOINT="<your region's OTLP endpoint>"
export OTEL_EXPORTER_OTLP_HEADERS="api-key=<license key>"
export OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE=delta
```

Keep keys out of source control: they belong in the host's secret store or environment, like any other secret (see [Configuration](../packages/Configuration/README.md)).

### Notable events

The toolkit has no title/text "event" API. A notable occurrence is a **structured log event with a stable `EventId`** and, when the count matters, a **counter**:

```csharp
telemetry.Log(LogLevel.Warning, caller, new EventId(4001, "order.payment_declined"), exception,
    "Payment declined for order {OrderId}", order.Id);
telemetry.IncrementCounter(caller, "payment_declined", 1, new("reason", "declined"));
```

To see them as Datadog Events or New Relic custom events, forward them from the logging pipeline: add a logging provider or OpenTelemetry log exporter that maps the `EventId` to the vendor's event, or decorate the registered `IMetricRecorder`. That mapping lives in your configuration, not in the toolkit.

### Serilog (optional)

Applications already on [Serilog](https://serilog.net/) keep it. `Anarchy.Toolkit.Infrastructure.Telemetry.Serilog` registers it as one more Microsoft logging provider, so toolkit logs reach the sinks you configured; instrumentation code does not change:

```csharp
services.AddSerilogTelemetry(config => config
    .Enrich.WithStructuredExceptionContext()
    .WriteTo.Console());                    // the container owns and flushes this logger

services.AddSerilogTelemetry(existingLogger);   // or an application-owned logger, never disposed here
```

Registration does not clear other providers, registers a given logger once, and ends with `AddCoreTelemetry()`, so existing telemetry registrations are kept. Command-line tools that own their output can call `AddSerilogConsoleTelemetry()` for a readable console logger, added only when no provider is registered. Libraries never add it. `AddTelemetryLogMetrics()` additionally writes each metric as a log event through the same pipeline, for backends that derive metrics from logs; metrics otherwise use the standard .NET pipeline and Serilog has no part in them.

Swapping Serilog for another provider (or back) changes only the composition root.

### Your company's setup

Put the organization's logging and metrics in one `ModuleTelemetryBase`, in your own library, and pass it to `ModuleTelemetry` (which `ModuleLibConfiguration` applies):

```csharp
public sealed class AcmeTelemetryModule : ModuleTelemetryBase
{
    protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddLogging(logging => logging.AddJsonConsole(options => options.IncludeScopes = true));
        services.AddSingleton<IMetricRecorder, AcmeMetricRecorder>();   // optional: a recorder of your own
    }
}

services.UseModule(
    new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(
        new ModuleTelemetry(new AcmeTelemetryModule(), config => new CorrelationIdOptions { CorrelationIdHeaderName = "X-Request-Id" })),
    configuration,
    environment);
```

The base calls `RegisterCoreTelemetry` first, then `AddCoreTelemetry()`, which keeps what you registered and fills in the rest. See [Bringing your company stack](bringing-your-company-stack.md).

An `IMetricRecorder` is four methods (counter, gauge, histogram, duration in seconds). Implement it only when the standard meter is not enough; to add behavior to the default, decorate the registered recorder.

## Correlation IDs

A correlation ID ties together the log lines and traces of one request; an attempt ID separates retries of it. They are provider-neutral and use two standard mechanisms:

- **A logging scope.** `ILogger.BeginScope` with the configured property names (`CorrelationId` and `AttemptId` by default). Every logging provider that supports scopes attaches them to the events written while the scope is open: the Microsoft console with `IncludeScopes`, Serilog, OpenTelemetry with `IncludeScopes`. It flows across `await`.
- **Activity tags.** The same values are set as tags on `Activity.Current`, so traces exported with OpenTelemetry carry them. They are tags, not baggage: baggage would implicitly propagate the values to downstream services, which this package does not do.

`CorrelationContext` also keeps the values in async-local storage, for code that needs to read or forward them.

| Entry point | How |
|---|---|
| HTTP request | `app.UseCorrelationId()` early in the pipeline (installed with the toolkit's web startups). Headers default to `X-Correlation-Id` and `X-Attempt-Id`; a missing header is generated unless the options say to ignore or reject it. |
| Worker iteration | `ResilientWorkerBase` opens a scope per iteration with a new correlation ID and attempt ID. Work that carries its own IDs opens `BeginCorrelationScope(correlationId, attemptId)` around its processing. |
| Any other entry point | `using (CorrelationContext.BeginScope(logger, correlationId, attemptId)) { ... }` |
| SQS message | `using (message.BeginCorrelationScope(logger)) { ... }` on the `InboundMessage` returned by `InboundMessageMapper.FromSqs(...)`. |

The AWS package maps inbound messages but ships no consumer loop, so nothing propagates IDs automatically: `BeginCorrelationScope` is the entry point your handler calls around each message. It reads the IDs from the message's string attributes (named `CorrelationId` and `AttemptId` unless you pass other names) and leaves an ID empty when the attribute is missing. Publishers do not add the attributes; copy `CorrelationContext.CorrelationId` into the outbound message attributes if consumers downstream need it.

Header names, property names and missing-header behavior are `CorrelationIdOptions`; pass a factory to `ModuleTelemetry` or `ModuleCorrelationId`. Coordinate header names with upstream and downstream services.

### With Serilog

A Serilog provider already receives the scope properties, so nothing more is needed. For events written outside such a scope, `Enrich.WithCorrelationId()` (in `Infrastructure.Telemetry.Serilog`) reads `CorrelationContext` and adds each property only when the event does not already have it, so it never duplicates the scope's properties. Its property names must match the ones the scopes use.

## Testing telemetry

`TestCaseBase.FixtureTelemetry` is a `TestTelemetry`: an `AppTelemetry` whose logs (with their properties and scopes) and metrics are captured in memory, so they can be asserted when telemetry *is* the behavior under test:

```csharp
var sut = new UserService(FixtureTelemetry.Telemetry);

await sut.CreateAsync(user, TestContext.Current.CancellationToken);

FixtureTelemetry.Logs.Should().Contain(log => log.Message.Contains("Created user"));
FixtureTelemetry.Metrics.CounterTotal("user_service.create_async.execution_count").Should().Be(1);
```

`services.AddTestTelemetry(FixtureTelemetry)` routes a container's telemetry to the same capture. Avoid asserting incidental logs or metrics in ordinary business-rule tests; assert the business result first. See [Testing Conventions](../conventions/testing.md).
