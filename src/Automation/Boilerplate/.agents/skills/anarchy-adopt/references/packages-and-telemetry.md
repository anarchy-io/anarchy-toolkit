# Individual packages and telemetry

Use this recipe when an existing application needs one capability without adopting the build. Choose from the
[package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) with the developer, then for each package:

```bash
dotnet add <Project>.csproj package Anarchy.Toolkit.<Package>
```

Read the package README (in the repository or, after restore, under `~/.nuget/packages/anarchy.toolkit.<package>/`) and
register its module as described. For relational data, install the matching provider
(`Infrastructure.Persistence.Db.Postgres`, `.Oracle` or `.Mysql`) and, in test projects, `Testing.Db.<Provider>`.
`Automation`, `Automation.Build` and `Automation.Build.Cli` are build tooling, never references from shipped application
code.

## Telemetry and company integrations

Toolkit packages use `AppTelemetry` through Microsoft logging and .NET metrics. The application chooses its providers
and exporters:

- **Install:** `Core.Telemetry` (already brought by `Configuration`, `Web` and `Hosting.Workers`); add
  `Infrastructure.Telemetry.Serilog` only when using Serilog, and `Infrastructure.CorrelationId` for correlation IDs.
- **Register:** `ModuleLibConfiguration` registers telemetry via `ModuleTelemetry`; otherwise call
  `services.AddCoreTelemetry()`. Inject `AppTelemetry` and use `this.CalledFrom()` call sites. Existing registrations
  are preserved; later registrations win.
- **Export:** configure logging providers with `AddLogging(...)` or `AddSerilogTelemetry(...)`; use OpenTelemetry with
  `AddMeter(CoreTelemetry.InstrumentationName)` and OTLP exporter settings in `OTEL_EXPORTER_OTLP_*`. Datadog and New
  Relic ingest OTLP without toolkit-specific packages. Put keys in secrets, never committed files.
- **Correlate:** `UseCorrelationId()` covers HTTP. Workers and message consumers open
  `CorrelationContext.BeginScope(...)`; mapped SQS messages can call `BeginCorrelationScope(logger)`.

The toolkit currently supplies only AWS SNS/SQS messaging. `Core.Messaging.Abstractions` is the broker-neutral
extension point for another transport. See [Bringing your company stack](guides/bringing-your-company-stack.md) for
organization-owned integrations and where their modules belong.

## Agent skills

Skills live in `.agents/skills`. With the developer's consent, install them with `anarchy-toolkit skills install` and
run `make skills_validate` when the build implements `IAgentSkillsBuild`. If the repository owns Make targets, use the
`anarchy-maintenance` guide to mark them and sync solution-specific skills.
