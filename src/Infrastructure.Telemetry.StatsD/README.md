# Anarchy.Toolkit.Infrastructure.Telemetry.StatsD

Optional DogStatsD integration for `Anarchy.Toolkit.Core.Telemetry`. It uses the
[DogStatsD .NET client](https://github.com/DataDog/dogstatsd-csharp-client), not a process-wide static client. The
application supplies the endpoint, prefix, global tags and other client settings; no application or organization
defaults are assumed.

```csharp
using Anarchy.Toolkit.Infrastructure.Telemetry.StatsD;
using StatsdClient;

services.AddStatsDTelemetry(new StatsdConfig {
    StatsdServerName = settings.MetricsHost,
    StatsdPort = settings.MetricsPort,
    Prefix = settings.MetricsPrefix
}, useDistributions: true);
```

Or pass `new ModuleStatsDTelemetry(config => /* build StatsdConfig from config */)` to `ModuleTelemetry`, or call
`AddStatsDTelemetry` from your own `ModuleTelemetryBase` alongside your logging setup. No configuration section is
assumed. It can be called before or after `AddCoreTelemetry`; the StatsD recorder becomes the active
`IMetricRecorder`, while a recorder registered later wins. A custom `ICoreTelemetry` or `AppTelemetry` is retained
and must use the recorder itself. This replaces the standard meter for toolkit metrics, rather than duplicating them
to both destinations; independently instrumented .NET meters are unaffected.

Counters and gauges map to DogStatsD counts and gauges. Histograms and durations map to DogStatsD histograms, or
distributions when `useDistributions` is true. Durations remain **seconds**, not DogStatsD timer milliseconds.
Non-null tags are formatted with invariant culture (booleans lowercase); null tags are omitted. Counts pass through
the client's double-valued API and can lose integer precision above 2^53. DogStatsD tags, histograms and distributions
are protocol extensions: verify that your StatsD-compatible receiver supports the selected metric types and tags.

The provider creates and configures its own client when first resolved, and disposes it (flushing buffered metrics)
with the service provider. Configuration failures throw on resolution. For short-lived or serverless invocations,
set the client's `SynchronousMode` and explicitly call `IDogStatsd.Flush()` at the invocation boundary; flushing only
at provider shutdown may be too late. Client-reported errors are logged as warnings; UDP delivery is best effort and
cannot confirm receipt. Follow the client's documentation for buffering, transport and environment variables.
`StatsDMetricRecorder` can also wrap a caller-owned,
already configured `IDogStatsd` directly and never disposes it.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
