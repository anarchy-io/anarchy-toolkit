# Onboarding Recipes

Short, self-contained recipes for setting up each piece of a toolkit-based solution for the first time: test projects, the mock server, modules, validated settings, health checks, workers and telemetry. Each fits in one pull request and ends with a "Done when" check. A solution created with the [CLI](../getting-started/cli-tool.md) already has most of them in place ([Your First Solution](greenfield-solution.md) walks through it); in a repository you already have, [Adopting an Existing Solution](adopting-existing-solution.md) puts them in order.

## Add an xUnit v3 test project

The toolkit's test kits (`Anarchy.Toolkit.Testing*`) and test targets expect [xUnit v3](https://xunit.net/) projects running on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) (MTP).

1. Create the project: `<OutputType>Exe</OutputType>`, a reference to `xunit.v3` and to the `Testing` kit your tests need (`Testing`, `Testing.Runtime.Web`, `Testing.Runtime.Web.Aws`, `Testing.Db.Postgres`, ...). The kits need [Moq](https://github.com/devlooped/moq) 4.21 or later and bring [AwesomeAssertions](https://awesomeassertions.org/).
2. Use the v3 companion packages when you need them: `AutoFixture.Xunit3`, `Snapshooter.Xunit3`.
3. Write tests as in the [Testing convention](../conventions/testing.md): derive from `TestCaseBase`, pass `TestContext.Current.CancellationToken` to calls that take a token ([xUnit1051](https://xunit.net/xunit.analyzers/rules/xUnit1051), an error in Release builds with warnings as errors), and keep `MemberData` members static.
4. Select the MTP runner in [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json): `{ "test": { "runner": "Microsoft.Testing.Platform" } }`.
5. If the CI server needs test results as files, add [`Microsoft.Testing.Extensions.TrxReport`](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-extensions-test-reports) and pass `--report-trx`.

A bootstrapped solution already has `tests/Unit`, `tests/Integration` and `tests/EndToEnd`, each referencing the shared, non-executable `tests/Common` project (renamed with the `tests.commonProject` preference); `anarchy-toolkit scaffold tests <kind>` adds a missing one.

Done when: `make test_unit` discovers and runs the project and reports its tests.

## Name test projects by kind

The build finds tests by kind: `Unit`, `Integration` and `EndToEnd`. Name a project after its kind (`tests/Unit/Unit.csproj`) or give it a segment for the kind plus `Tests` (`Shop.Tests.Unit`), and keep one project per kind: projects do not run in parallel with each other, while the tests inside one do.

Keep unit tests free of external resources: packing and container publishing run them, and never the other kinds. A different naming scheme is declared, not renamed: `"TestKindAliases": ["EndToEnd=Acceptance"]` in `.fallout/parameters.json`, or an override of `IsTestProjectOfKind` in `build/Build.cs` (see [Anarchy automation builds](automation-builds.md#how-istandardbuild-finds-test-projects)).

Done when: `make test_unit`, `make test_integration` and `make test_end_to_end` each run exactly the projects of their kind.

## Add the mock server

Use it to stand in for HTTP upstreams in integration and end-to-end tests, without Docker.

1. Add a runnable project whose name ends in `MockServer` (for example `tests/MockServer`, which `anarchy-toolkit bootstrap --with-mock-server` creates), referencing `Anarchy.Toolkit.Runtime.Web.MockServer`, with `return await MockServerEntryPoint.RunAsync(args);` as its entry point.
2. Put expectation files (JSON in MockServer's format) in a folder per upstream, and add `IMockServerBuild` to the build with `"MockServerExpectations": ["tests/MockServer/mocks"]`.
3. Pin the port your test settings use (`"MockServerPort": 6001`): an explicit port fails loudly when it is taken.
4. Point the applications' development and test settings at it.
5. In the integration and end-to-end tests, reference `Testing.MockServer` and call `ShouldAllHaveBeenMatched()` on the recorded requests in an assembly fixture, so an unmocked call fails the run.

Done when: `make test_end_to_end_mocked` passes, and removing one expectation on purpose fails it. More: [Mock Server](mock-server.md).

## Write a module

One `IDependencyInjectionModule` per feature or adapter, in `DependencyInjection/Module/Module<Name>.cs`. The module receives its options from the composition root instead of reading a section itself:

```csharp
public sealed class ModuleCarrierClient(Func<IConfiguration, CarrierClientSettings> settings) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var options = settings(configuration);
        services.AddSingleton(options);
        services.AddHttpClient<ICarrierClient, CarrierClient>(client => client.BaseAddress = options.BaseAddress);
        return services;
    }
}

services.UseModule(new ModuleCarrierClient(c => c.Get<AppSettings>()!.CarrierClient), configuration, environment);
```

Add a test that registers the module on an empty `ServiceCollection` and resolves it with `ValidateOnBuild`. If a plain `Add*` extension is useful on its own, the module calls it. See [Dependency Injection](../conventions/dependency-injection.md).

Done when: the module test passes and the application resolves the service at startup.

## Add validated settings

1. Add `AppSettings : IAppSettings` with one property per section, each marked `[Required, ValidateObjectMembers]` where it must be present.
2. Add `ModuleAppSettings : ModuleAppSettingsBase<AppSettings>` and register `ModuleLibConfiguration<ModuleAppSettings, AppSettings>`, which also registers telemetry and, locally, in-memory secrets; opt into a remote secret store with `.WithSecretManagement(new ModuleAwsSecretManagement())` from `Bootstrap.Aws`. In a bootstrapped monorepo with shared settings, these already exist once: `AppSettings` in the Infrastructure library, `ModuleAppSettings` and the `appsettings*.json` files in the Bootstrap library.
3. List the keys a developer must provide in `appsettings.json.template`; `make appsettings` asks for them.
4. Add a test that boots the settings from each environment's configuration, and one that removes a section and expects the boot to fail.

[`ValidateDataAnnotations`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.optionsbuilderdataannotationsextensions.validatedataannotations) does not recurse into nested objects: validate them from `AppSettings` ([`IValidatableObject`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject)).

Done when: the application fails to start when a required section is missing, and starts with the template's keys filled in.

## Add a health check

One class per dependency: derive `HealthCheckBase`, set `Name` and implement `CheckAsync` (return to pass, throw to fail; timing and failures are handled). Register it as an `IHealthCheck`, or use a provider's ready-made reports (`ModulePostgresHealthReport`). Web hosts serve them through the health controller, workers through `AddServiceHealthCheckWorker(assembly)` at `/health/instance`, `/health/services` and `/health/status`.

Done when: the services endpoint lists the check, and returns HTTP 500 when the dependency is unavailable.

## Add a worker

Derive `MonitoredWorkerBase` and implement `RunIterationAsync`, returning `true` when it did work (the next run follows the recurring interval) or `false` when idle (the delayed interval). The loop, cancellation, failure logging and a `health.ping` metric come with it. Register the worker with `services.AddHostedService<TWorker>()` in a module. Work that must finish before the first request (a cache preload) goes in a hosted service's `StartAsync`, registered before the web host starts.

Done when: the worker logs and counts each iteration through `AppTelemetry`, stops cleanly on cancellation, and its metrics appear in your backend.

## Register telemetry

`ModuleLibConfiguration` already registers `AppTelemetry`. Add your logging providers and, for metrics and traces, an OpenTelemetry OTLP exporter listening to the toolkit meter; package them as a `ModuleTelemetryBase` when several solutions share the setup. See [Observability](observability.md) and [Bringing your company stack](bringing-your-company-stack.md).

Done when: a log line and a metric written through `AppTelemetry` reach your backend, with the correlation ID on the log line.
