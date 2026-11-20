# Bringing Your Company Stack

The toolkit is [vendor-neutral](https://anarchy-io.github.io/anarchy-toolkit/decisions/#values): it ships no [Datadog](https://www.datadoghq.com/) or New Relic SDK, no company auth service, no deployment tool. Every organization adds those once, through extension points, and every solution of that organization reuses them. This guide lists the extension points and where the code goes.

## Where company code lives

Keep it in your own libraries, in the layer it belongs to, under a folder named after the company:

```text
src/Library/
├── Infrastructure/Acme/     metric recorders, auth handlers, correlation propagation
└── Configuration/Acme/      ModuleAcmeConfiguration, AcmeTelemetryModule, config sources
```

Once two solutions need it, move it to a shared internal package (`Acme.Toolkit.*`) on your private feed. Until then, copying it with an `// Origin: <repo>/<path>` header keeps track of where each copy came from.

## Extension points

| Concern | Extension point | Guide |
|---|---|---|
| Logging providers (Serilog, JSON console, a vendor's provider) | Register them in a `ModuleTelemetryBase` (`RegisterCoreTelemetry`), or with `AddLogging(...)` / `AddSerilogTelemetry(...)` | [Observability](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html#your-companys-setup) |
| Metrics and traces backend (Datadog, New Relic, [Prometheus](https://prometheus.io/), CloudWatch) | An OpenTelemetry OTLP exporter listening to `CoreTelemetry.InstrumentationName`, configured with `OTEL_EXPORTER_OTLP_*` | [Observability](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html#datadog-and-new-relic) |
| A metrics client the exporter cannot reach | Your own `IMetricRecorder` registered in DI, or a decorator around the default | [Observability](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html#your-companys-setup) |
| Vendor events (Datadog Events, New Relic custom events) | A logging provider or log exporter that maps the toolkit's structured log events (stable `EventId`) | [Observability](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html#notable-events) |
| Log property names, correlation headers | `CorrelationIdOptions` passed to `ModuleTelemetry` / `ModuleCorrelationId`; standard properties through your provider's enrichers | [Infrastructure.CorrelationId](https://anarchy-io.github.io/anarchy-toolkit/packages/Infrastructure.CorrelationId/) |
| Configuration sources and secrets | `ConfigureAppConfiguration` hooks on the hosts | [Hosting](https://anarchy-io.github.io/anarchy-toolkit/packages/Hosting/) |
| Outbound auth (token handlers) | A [`DelegatingHandler`](https://learn.microsoft.com/dotnet/api/system.net.http.delegatinghandler) registered in your own module | [Infrastructure.Http](https://anarchy-io.github.io/anarchy-toolkit/packages/Infrastructure.Http/) |
| Inbound auth in tests | Mock-server expectations for your identity provider and an unsigned-token handler in test settings | [Mock Server](https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html#your-own-host) |
| Private NuGet feeds | `nuget.config` with package source mapping; login scripts as extra make targets | [Anarchy automation builds](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html) |
| Deployment ([CodeDeploy](https://aws.amazon.com/codedeploy/), Lambda zips, your deploy tool) | A separate release build next to `build/`, with its own make targets | [ADR 0006](https://anarchy-io.github.io/anarchy-toolkit/decisions/0006-fallout-build.html) |
| Messaging transport | `Core.Messaging.Abstractions` defines the broker-neutral extension point. AWS SNS/SQS in `Infrastructure.Messaging.Aws` is currently the only supplied transport; other brokers require an adapter and, optionally, a matching test kit. | [Package Catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/#infrastructure-adapters-to-external-systems) |
| Pipeline-specific targets | Your own `.mk` files in `MAKE_EXTRA_TARGETS` | [Adopting an existing solution](adopting-existing-solution.md#anarchy-automation-next-to-your-pipeline) |

## One module for all of it

Wrap the company's logging and metrics in one `ModuleTelemetryBase`, then the whole company stack in one module applied first by every application:

```csharp
public sealed class AcmeTelemetryModule : ModuleTelemetryBase
{
    protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddLogging(logging => logging.AddJsonConsole(options => options.IncludeScopes = true));
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddMeter(CoreTelemetry.InstrumentationName).AddOtlpExporter())
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddOtlpExporter());
    }
}

public sealed class ModuleAcmeConfiguration : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services.UseModule(
            new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(
                new ModuleTelemetry(new AcmeTelemetryModule(), _ => new CorrelationIdOptions { CorrelationIdHeaderName = "X-Request-Id" })),
            configuration,
            environment);
}
```

The destination (endpoint, API key, temporality) is the exporter's configuration, usually the `OTEL_EXPORTER_OTLP_*` environment variables, so the same module serves every environment. Keep keys in the host's secret store.

## Offering it back

A gap that isn't company-specific (a forwarding middleware for the mock server, an outbound correlation handler) belongs upstream: open an issue or a pull request on the toolkit.
