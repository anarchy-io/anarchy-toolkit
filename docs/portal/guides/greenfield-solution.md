# Your First Solution

A walkthrough from an empty folder to a solution with a shared library per layer, an API, a job, a mock server for its upstreams and a pull-request gate. It follows the path a real team took to start a monorepo for two services on the platform. The [CLI tool page](../getting-started/cli-tool.md) is the reference for every command used here.

## Bootstrap

```bash
dotnet tool install --global Anarchy.Toolkit.Automation.Build.Cli
anarchy-toolkit bootstrap
```

The wizard asks whether you are creating one application or several before it asks about layering mode. A single application gets one application-folder mapping; several applications start with the shared monorepo layout. For each API it asks whether to include Swagger (`Anarchy.Toolkit.Runtime.Web.Swagger`) and the toolkit's Swagger UI theme. In a monorepo it asks whether the applications share their settings (one `AppSettings` in the Infrastructure library, one `ModuleAppSettings` and the `appsettings*.json` files in the Bootstrap library, linked into every application by the root `Directory.Build.props`) or keep them isolated. It then offers the Unit, Integration and EndToEnd test projects (with a shared `tests/Common` project), a mock server host in `tests/MockServer` and the agent skills, and shows the plan before writing anything. The non-interactive equivalents are `--no-tests`, `--with-mock-server`, `--isolated-app-settings` and `--with-skills`. Then:

```bash
make install
make pull_request
```

Remove `ILayeringBuild` from `build/Build.cs` to opt out; to adapt your layout, use [layering preferences](automation-preferences.md) or follow the [layering convention's opt-out guidance](../conventions/layering.md#opting-out-or-adapting).

You now have `build/` (Anarchy automation powered by [Fallout Build](https://fallout.build/), with `build/Build.cs` as its entry point), a Makefile and scripts, [`Directory.Packages.props`](https://learn.microsoft.com/nuget/consume-packages/central-package-management), a library per layer except `Apps`, your applications and the test projects, all compiling and tested. Namespaces start with the solution name (`Shop.Library.Infrastructure`, `Shop.Apps.Apis.Orders`, `Shop.Tests.Unit`). In the solution, the root files (Makefile, scripts, `Directory.Build.props`, `Directory.Packages.props`, ...) sit in a `solution items` folder, and the `_build` project sits at the solution root together with `.fallout/parameters.json`. No `samples` folder is created.

## Choose a layout

The wizard's first real decision is how projects map to layers.

| Layout | Looks like | Pick it when |
|---|---|---|
| **By folder** (default) | `src/Library/Core/Core.csproj`, `src/Library/Runtime/Runtime.csproj`, `src/Apps/Apis/Orders/...` | One or several applications share a library per layer; few, larger projects. |
| **By name prefix** | `src/Library.Core/`, `src/Library.Infrastructure/`, `src/Apps.Api.Orders/` | Useful for separately packaged projects (like the toolkit itself), but available to any solution. |

Choose one mode for the entire solution; the check does not mix modes or fall back between them. Folder-based layering
is recommended for adopters. For a single application, layer projects can sit directly under `src/` rather than under
`src/Library/`. See [Code Organization](../conventions/code-organization.md) for the layouts.

The chosen layout applies to this bootstrap/scaffold operation. Saving it is optional: pass `--save-preferences` or
accept the wizard's separate save prompt (default **No**) to write selected preferences to the root
`.anarchy-toolkit.json`. Without saving, later commands can use the same explicit layout arguments. With folder
layering, preferences can override where the libraries live and which layer each application folder belongs to:

```json
{
  "$schema": "https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json",
  "layering": {
    "mode": "folder",
    "libraryRoot": "src/Library",
    "applicationFolders": { "src/Apps/Apis": "Apps", "src/Apps/Jobs": "Apps", "src/Apps/Ui": "Apps" }
  }
}
```

For a single application, set `"libraryRoot": "src"` and map the application's folder (e.g. `{ "src/App": "Apps" }`) as an operation choice or saved override. With no configuration, the standard `src/Library` folder baseline is used; mode is never inferred.

For non-interactive setup, choose a preset directly:

```bash
anarchy-toolkit bootstrap Shop --layout single-app --application-folder Shop --save-preferences
anarchy-toolkit scaffold api Orders --layout single-app --application-folder Shop
```

`single-app` puts the layer libraries under `src/` and maps one application folder to the `Apps` layer. The folder defaults to `App`; choose names such as `Api`, `Job`, `Cli` or the solution name with
`--application-folder`. `monorepo` keeps the standard shared `src/Library` and `src/Apps/Apis`, `src/Apps/Jobs`, `src/Apps/Ui` layout.
Both presets accept the regular layering options for custom roots, layers and mappings. See the [preferences guide](automation-preferences.md)
for what belongs in each settings file and how one-run arguments differ from saved defaults.

No library can reference an application. Several domains can share one project per layer, side by side in folders (`Core/Orders/`, `Core/Billing/`, `Core/Common/`). See the [Layering convention](../conventions/layering.md) for the rules and how to bend them.

Prefer placing Anarchy and other third-party dependencies in the shared libraries, with DI modules or wrappers where
useful, so applications can build on a common base. This can pull unused transitive assemblies into an application's
artifacts; the solution developer decides when application-local dependencies are a better trade-off.

## Add applications

```bash
anarchy-toolkit scaffold api Orders
anarchy-toolkit scaffold job InvoiceCleanup
```

APIs include Swagger and its theme unless you pass `--no-swagger` or `--no-swagger-theme` (or save `scaffolding.swagger` / `scaffolding.swaggerTheme` as `false`). `--container` adds a container image to an API or UI, `--lambda-api` makes an API an AWS Lambda, `--lambda-event` makes a job an event-driven Lambda, and `--job-mode worker|once` picks a long-running worker or a run-once job. New applications follow the `scaffolding.appSettings` preference (`shared` or `isolated`).

Each application is a thin entry point: a `Program`, a `Startup` choosing modules, and settings. The logic goes in the libraries. Describe the settings once in an `AppSettings` class validated on start, and list the keys a developer must provide in `appsettings.json.template`; `make appsettings` asks for them. See [Dependency Injection](../conventions/dependency-injection.md).

## Add libraries and test suites

Add projects to the effective layout instead of creating directories by hand:

```bash
anarchy-toolkit scaffold library Orders --layer Core
anarchy-toolkit scaffold library Postgres --layer Infrastructure
anarchy-toolkit scaffold tests unit
```

Folder mode places libraries under the configured `libraryRoot`; prefix mode names them `<Layer>.<Name>` under the
checked root. A new library references the immediately lower layer's project when it exists. `scaffold tests` creates
one shared project per kind below `TestsRoot` (or `tests`) and uses the kind name the build recognizes, including a
configured `TestKindAliases` alias. Use `--dry-run` to preview either project without writing. `--save-preferences`
only saves explicit layout choices; it does not persist test names or other operation inputs.

## Stand in for upstreams

Bootstrap with `--with-mock-server` (or answer yes in the wizard) to get `tests/MockServer` (with the UI, and expectations under `tests/MockServer/mocks`), or add a runnable project whose name ends in [`MockServer`](https://www.mock-server.com/) referencing `Anarchy.Toolkit.Runtime.Web.MockServer`, put expectation files under a folder per upstream, and add `IMockServerBuild` to the build:

```json
{ "MockServerExpectations": ["tests/MockServer/mocks"] }
```

`make mock_server_start` runs it for local development; `make test_end_to_end_mocked` runs it around the tests. Point the applications' development and test settings at it. The [Mock Server guide](mock-server.md) covers runtime expectations, assertions and recording.

## Bring your company stack

Logging sinks, metrics backend, correlation headers, CodeArtifact or other private feeds, auth: see [Bringing your company stack](bringing-your-company-stack.md). Keep that code in your own `Bootstrap` and `Infrastructure` libraries, in a folder named after the company, so it is easy to find and to replace.

## Add databases and brokers when you need them

Add a `docker/docker-compose.yml` and `IDockerBuild` (`make start`, `stop`, `destroy`), then the persistence or messaging package and its `Testing.*` kit. See [Database Providers](database-providers.md) and the [Package Catalog](../packages/README.md).

## Wire the pipeline

The pull-request pipeline runs one command:

```bash
make pull_request
```

If you deploy with something the toolkit doesn't ship ([CodeDeploy](https://aws.amazon.com/codedeploy/), Lambda zips, a company deploy tool), keep it in a separate release build next to `build/`, with its own make targets. See [Anarchy automation builds](automation-builds.md) for packages and container images.

## Write down your deviations

Your solution will differ from the conventions somewhere: an exception middleware with your own mapping, a different test layout, a pinned driver. Record each one in an ADR or in `AGENTS.md`, linking the convention you deviate from. Coding agents and new team members then know which rule wins.

## Checklist

- [ ] `make pull_request` passes on a clean clone.
- [ ] `make check_layering` passes, with application folders mapped.
- [ ] Settings validated on start; `appsettings.json.template` lists the local keys.
- [ ] Upstreams mocked; `ShouldAllHaveBeenMatched` fails on unmocked calls.
- [ ] Company specifics isolated in their own folder.
- [ ] Deviations from the conventions recorded.
