# Pitfalls

Things that surprised teams adopting the platform, and what to do about them.

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet test` refuses some test projects | [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json) selects the [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) runner, and those projects are [VSTest](https://github.com/microsoft/vstest) projects. | Make them [xUnit v3](https://xunit.net/) projects ([recipe](onboarding-recipes.md#add-an-xunit-v3-test-project)), or leave the runner out of `global.json`; `make test_*` runs each project with its own runner. |
| `make test_end_to_end` runs nothing | Test projects named outside the kinds (`Acceptance`, `Specs`). | `TestKindAliases`, see [recipes](onboarding-recipes.md#name-test-projects-by-kind). |
| Package downgrade or conflict errors with `Testing.*` kits | [Moq](https://github.com/devlooped/moq) pinned below 4.21, or xUnit v2 in the same project. | Moq 4.21 or later; xUnit v3. |
| A dependency changes version after adding a toolkit package | Transitive upgrades ([Serilog](https://serilog.net/), [Polly](https://www.pollydocs.org/), AWS SDK, database drivers). | Check `dotnet list package --include-transitive`; pin in [`Directory.Packages.props`](https://learn.microsoft.com/nuget/consume-packages/central-package-management) if needed, and note the reason there. |
| The mock server starts on another port | No explicit port, and the default one is taken. | Pin `MockServerPort` to the port your settings use, so it fails instead. |
| Unmocked upstream calls go unnoticed | Nothing checks the recorded requests. | `ShouldAllHaveBeenMatched()` in an assembly fixture. |
| `CS0118` or `CS0104` around `Attribute` or `Host` | Responsibility folders produce namespaces named like types. | An alias after the file-scoped namespace; see [Layering](../conventions/layering.md#namespace-collisions-to-know-about). |
| `make check_layering` reports hundreds of violations | The solution isn't laid out by the default folder baseline. | Leave `ILayeringBuild` out for now, pass explicit layout arguments, or override `layering.libraryRoot` and mappings in `.anarchy-toolkit.json`; see [Layering: opting out](../conventions/layering.md#opting-out-or-adapting). |
| Toolkit metrics never reach the backend | The OpenTelemetry meter provider does not listen to the toolkit's meter. | `AddMeter(CoreTelemetry.InstrumentationName)` in `WithMetrics`; see [Observability](observability.md#opentelemetry-and-otlp-preferred). |
| Correlation IDs are missing from exported or console logs | The logging provider ignores scopes. | Enable them: `IncludeScopes = true` on the console formatter or `OpenTelemetryLoggerOptions`; Serilog providers take them automatically. See [Correlation IDs](observability.md#correlation-ids). |
| Correlation IDs are missing from SQS message logs | Nothing opens the scope: the AWS package maps messages but ships no consumer loop. | Wrap each message's handling in `message.BeginCorrelationScope(logger)`. |
| Logs go nowhere in a console tool | The default registration adds no sink. | Register a provider, or `AddSerilogConsoleTelemetry()` in the tool's composition root. |
| Boot fails after adding `AppSettings` | Validation attributes that some environment doesn't satisfy. | Annotate only what every environment already provides; boot each environment's settings in a test. |
| A load balancer marks instances unhealthy after health checks are added | The toolkit's health paths or response bodies differ from the ones it polls. | Check which paths and bodies it expects, and point it at the toolkit's endpoints (`/health/*`) or serve the expected ones. |
| Analyzer errors only in CI | They only fail with `-c Release -p:TreatWarningsAsErrors=true`. | Build that way locally before pushing. |
| Nullable warnings in code without nullable annotations | The solution enables them and the code doesn't annotate. | `<Nullable>disable</Nullable>` per project, or `#nullable disable` per file, until it is cleaned up. |
| `ArgumentException` comes back as `500` | `ExceptionMiddleware` maps by native base type. | Throw the toolkit's specialized types, or use your own exception handler; see [Exceptions](../conventions/exceptions.md#opting-out-or-adapting). |
