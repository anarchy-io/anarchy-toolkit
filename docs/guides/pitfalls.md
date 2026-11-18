# Pitfalls

Things that surprised teams adopting the platform, and what to do about them.

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet test` refuses some test projects | [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json) selects the [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) runner, and those projects are still on [VSTest](https://github.com/microsoft/vstest). | Leave the runner out of `global.json` until every project is on [xUnit v3](https://xunit.net/); `make test_*` runs both meanwhile. |
| `make test_end_to_end` runs nothing | Test projects named outside the kinds (`Acceptance`, `Specs`). | `TestKindAliases`, see [recipes](migration-recipes.md#test-project-names-to-test-kinds). |
| Package downgrade or conflict errors with `Testing.*` kits | [Moq](https://github.com/devlooped/moq) pinned below 4.21, or xUnit v2 in the same project. | Moq 4.21 or later; xUnit v3. |
| A dependency changes version after adding a toolkit package | Transitive upgrades ([Serilog](https://serilog.net/), [Polly](https://www.pollydocs.org/), AWS SDK, database drivers). | Compare `dotnet list package --include-transitive` before and after; pin in [`Directory.Packages.props`](https://learn.microsoft.com/nuget/consume-packages/central-package-management) if needed, and note the reason there. |
| The mock server starts on another port | No explicit port, and the default one is taken. | Pin `MockServerPort` to the port your settings use, so it fails instead. |
| Unmocked upstream calls go unnoticed | Nothing checks the recorded requests. | `ShouldAllHaveBeenMatched()` in an assembly fixture. |
| `CS0118` or `CS0104` around `Event`, `Attribute` or `Host` | Responsibility folders produce namespaces named like types. | An alias after the file-scoped namespace; see [Layering](../conventions/layering.md#namespace-collisions-to-know-about). |
| `make check_layering` reports hundreds of violations | The solution isn't laid out by layers. | Leave `ILayeringBuild` out for now, or use folder mode with a `LayeringRoot`; see [Layering: opting out](../conventions/layering.md#opting-out-or-adapting). |
| Boot fails after adding `AppSettings` | Validation attributes that some environment doesn't satisfy. | Annotate only what every environment already provides; boot each environment's settings in a test. |
| Load balancer marks instances unhealthy after moving health checks | Different health paths or response bodies. | Compare old and new endpoints first; add aliases. |
| Analyzer errors only in CI | They only fail with `-c Release -p:TreatWarningsAsErrors=true`. | Build that way locally before pushing. |
| Nullable warnings when moving code between solutions | One solution enables nullable reference types, the other doesn't. | `<Nullable>disable</Nullable>` per moved project, or `#nullable disable` per file, until it is cleaned up. |
| `ArgumentException` comes back as `500` | `ExceptionMiddleware` maps by native base type. | Throw the toolkit's specialized types, or use your own exception handler; see [Exceptions](../conventions/exceptions.md#opting-out-or-adapting). |
