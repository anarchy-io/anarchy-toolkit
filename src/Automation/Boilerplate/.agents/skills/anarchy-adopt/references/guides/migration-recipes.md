# Migration Recipes

Short, self-contained recipes for the moves most adoptions make. Each keeps behaviour identical and fits in one pull request. The [adoption walkthrough](adopting-existing-solution.md) puts them in order.

## xUnit v2 and VSTest to xUnit v3 and MTP

1. Move one group of test projects per change; record the test counts first.
2. In each project: `<OutputType>Exe</OutputType>`, `xunit` → `xunit.v3`, `AutoFixture.Xunit2` → `AutoFixture.Xunit3`, `Snapshooter.Xunit` → `Snapshooter.Xunit3` (same `.snap` files), [Moq](https://github.com/devlooped/moq) 4.21 or later. Drop [VSTest](https://github.com/microsoft/vstest) adapters such as `TeamCity.VSTest.TestAdapter` and `coverlet.collector`.
3. Code: `ITestOutputHelper` moved to `Xunit`, `IAsyncLifetime` returns `ValueTask`, pass `TestContext.Current.CancellationToken` ([xUnit1051](https://xunit.net/xunit.analyzers/rules/xUnit1051)), `MemberData` must be static.
4. CI results: MTP doesn't use VSTest loggers; publish TRX files ([`Microsoft.Testing.Extensions.TrxReport`](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-extensions-test-reports), `--report-trx`) if your CI server needs them.
5. With the last group, set the runner in [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json): `{ "test": { "runner": "Microsoft.Testing.Platform" } }`. Before that, `dotnet test` would refuse the remaining VSTest projects; `make test_*` runs both kinds meanwhile.

Done when: same counts, no snapshot changes, CI shows the results. Details: the `anarchy-adopt` skill's xUnit reference.

## Test project names to test kinds

Projects named `*.Tests.Acceptance`, `*.Specs` or similar are not found by `make test_end_to_end`. Either:

- map them: `"TestKindAliases": ["EndToEnd=Acceptance"]` in `.fallout/parameters.json`; or
- rename them in a separate, rename-only pull request; or
- override `IsTestProjectOfKind` in `build/Build.cs`.

Keep unit tests free of external resources: packing and container publishing run them.

## MockServer container to the toolkit mock server

1. Add a runnable project ending in [`MockServer`](https://www.mock-server.com/) with `return await MockServerEntryPoint.RunAsync(args);`, and `IMockServerBuild` in the build.
2. `git mv` the expectation files unchanged and point `MockServerExpectations` at them.
3. Pin the port your settings use (`"MockServerPort": 6001`): an explicit port fails loudly when taken.
4. Replace `/mockserver/verify` and catch-all "unmatched" expectations with `ShouldAllHaveBeenMatched()` in an assembly fixture of the integration and end-to-end tests.
5. Replace `make start` around tests with `make test_end_to_end_mocked`; `MockServerFixture` spawns the server from an IDE.
6. Remove the container from [Docker Compose](https://docs.docker.com/compose/). Keep it only for proxy recording until you add a [recording middleware](https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html#recording-a-real-upstream).

Done when: same test counts, and removing one expectation on purpose fails the run. Full detail: [Mock Server](https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html#migrating-from-a-mockserver-container).

## `Add*` extensions to modules

```csharp
// Before: reads its own section
public static IServiceCollection AddCarrierClient(this IServiceCollection services, IConfiguration config) { ... config.GetRequired<CarrierClientSettings>("CarrierClient") ... }

// After: the caller passes the read
public sealed class ModuleCarrierClient(Func<IConfiguration, CarrierClientSettings> settings) : IDependencyInjectionModule { ... }

services.UseModule(new ModuleCarrierClient(c => c.GetRequired<CarrierClientSettings>("CarrierClient")), configuration, environment);
```

One extension per change, smallest first; keep the old extension delegating to the module until no caller is left. Add a test that registers the module on an empty `ServiceCollection` and resolves it with `ValidateOnBuild`. Modules needing a service that used to be passed as an argument (a logger, a facade) resolve it from the container instead. See [Dependency Injection](https://anarchy-io.github.io/anarchy-toolkit/conventions/dependency-injection.html).

## `IOptions<T>` to validated `AppSettings`

1. Add `AppSettings : IAppSettings` with one property per existing section, `[Required, ValidateObjectMembers]`.
2. Register `ModuleAppSettings : ModuleAppSettingsBase<AppSettings>` next to the current binding. Nothing reads it yet.
3. Boot every environment's settings in a test; add one test that removes a section and expects the boot to fail.
4. Move modules to `c => c.Get<AppSettings>()!.Section` one by one.

[`ValidateDataAnnotations`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.optionsbuilderdataannotationsextensions.validatedataannotations) doesn't recurse into nested objects: validate them from `AppSettings` ([`IValidatableObject`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject)).

## Health checks to `ServiceHealthReportBase`

One class per dependency: set `Name`, implement `CheckAsync` (return to pass, throw to fail). **First compare the health paths and JSON** of the old and new endpoints: load balancers, smoke tests and dashboards read them. Add aliases if they differ.

## Hosted services to `MonitoredWorkerBase`

Implement `RunIterationAsync`, returning `true` when it did work. The loop, cancellation, failure logging and a `health.ping` metric come with it; check the new metric doesn't collide with an existing monitor. Work that must finish before the first request (a cache preload) goes in a hosted service's `StartAsync`, registered before the web host starts.
