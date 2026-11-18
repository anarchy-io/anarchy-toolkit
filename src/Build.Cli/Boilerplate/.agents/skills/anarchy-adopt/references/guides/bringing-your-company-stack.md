# Bringing Your Company Stack

The toolkit is [vendor-neutral](https://anarchy-io.github.io/anarchy-toolkit/decisions/#values): it ships no [Datadog](https://www.datadoghq.com/), no company auth service, no deployment tool. Every organization adds those once, through extension points, and every solution of that organization reuses them. This guide lists the extension points and where the code goes.

## Where company code lives

Keep it in your own libraries, in the layer it belongs to, under a folder named after the company:

```text
src/Library/
├── Infrastructure/Acme/     measurers, auth handlers, correlation propagation
└── Configuration/Acme/      ModuleAcmeConfiguration, AcmeObserverFactory, config sources
```

Once two solutions need it, move it to a shared internal package (`Acme.Toolkit.*`) on your private feed. Until then, copying it with an `// Origin: <repo>/<path>` header keeps track of where each copy came from.

## Extension points

| Concern | Extension point | Guide |
|---|---|---|
| Log sinks, enrichers, property names | Derive `ObserverFactory`: `Enrich`, `WriteTo`, `CorrelationIdPropertyName` | [Observability](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html#plugging-in-an-organizations-setup) |
| Metrics backend (Datadog, [Prometheus](https://prometheus.io/), CloudWatch) | `ObserverFactory.CreateMeasurer` returning your `MeasurerBase` | [Observability](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html#plugging-in-an-organizations-setup) |
| Correlation headers | `CorrelationIdOptions` passed to `ModuleObservability` / `ModuleCorrelationId` | [Infrastructure.CorrelationId](https://anarchy-io.github.io/anarchy-toolkit/packages/Infrastructure.CorrelationId/) |
| Configuration sources and secrets | `ConfigureAppConfiguration` hooks on the hosts | [Hosting](https://anarchy-io.github.io/anarchy-toolkit/packages/Hosting/) |
| Outbound auth (token handlers) | A [`DelegatingHandler`](https://learn.microsoft.com/dotnet/api/system.net.http.delegatinghandler) registered in your own module | [Infrastructure.Http](https://anarchy-io.github.io/anarchy-toolkit/packages/Infrastructure.Http/) |
| Inbound auth in tests | Mock-server expectations for your identity provider and an unsigned-token handler in test settings | [Mock Server](https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html#your-own-host) |
| Private NuGet feeds | `nuget.config` with package source mapping; login scripts as extra make targets | [Fallout builds](https://anarchy-io.github.io/anarchy-toolkit/guides/fallout-builds.html) |
| Deployment ([CodeDeploy](https://aws.amazon.com/codedeploy/), Lambda zips, your deploy tool) | A separate release build next to `build/`, with its own make targets | [ADR 0006](https://anarchy-io.github.io/anarchy-toolkit/decisions/0006-fallout-build.html) |
| Pipeline-specific targets | Your own `.mk` files in `MAKE_EXTRA_TARGETS` | [Adopting an existing solution](adopting-existing-solution.md#the-fallout-build-next-to-what-you-have) |

## One module for all of it

Wrap the company pieces in one module, applied first by every application:

```csharp
public sealed class ModuleAcmeConfiguration : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services.UseModule(
            new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(
                new ModuleObservability(new AcmeObserverFactory(), _ => new CorrelationIdOptions { CorrelationIdHeaderName = "X-Request-Id" })),
            configuration,
            environment);
}
```

## Migrating from a company monitoring facade

Build the old facade on top of the logger and metrics client your `ObserverFactory` creates, so both write to the same place with the same properties. Then convert call sites to `Observer` in their own changes, never in the same change as DI or startup work, and compare one log event and the metric names before and after on a test environment.

## Offering it back

A gap that isn't company-specific (a forwarding middleware for the mock server, an outbound correlation handler) belongs upstream: open an issue or a pull request on the toolkit.
