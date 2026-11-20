# Your First Solution

A walkthrough from an empty folder to a solution with a shared library per layer, an API, a job, a mock server for its upstreams and a pull-request gate. It follows the path a real team took to start a monorepo for two services on the platform. The [CLI tool page](https://anarchy-io.github.io/anarchy-toolkit/getting-started/cli-tool.html) is the reference for every command used here.

## Bootstrap

```bash
dotnet tool install --global Anarchy.Toolkit.Automation.Build.Cli
anarchy-toolkit bootstrap
```

The wizard asks whether you are creating one application or several before it asks about layering mode. A single application gets one application-folder mapping; several applications start with the shared monorepo layout. It shows the plan before writing anything. Then:

```bash
make install
make pull_request
```

Remove `ILayeringBuild` from `build/Build.cs` to opt out; to adapt your layout, use [layering preferences](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html) or follow the [layering convention's opt-out guidance](https://anarchy-io.github.io/anarchy-toolkit/conventions/layering.html#opting-out-or-adapting).

You now have `build/` (Anarchy automation powered by [Fallout Build](https://fallout.build/)), a Makefile and scripts, [`Directory.Packages.props`](https://learn.microsoft.com/nuget/consume-packages/central-package-management), a library per layer and your applications, all compiling and tested.

## Choose a layout

The wizard's first real decision is how projects map to layers.

| Layout | Looks like | Pick it when |
|---|---|---|
| **By folder** (default) | `src/Library/Core/Core.csproj`, `src/Library/Web/Web.csproj`, `src/Api/Orders/...` | One or several applications share a library per layer; few, larger projects. |
| **By name prefix** | `src/Core.Orders/`, `src/Infrastructure.Orders.Postgres/` | Useful for separately packaged projects (like the toolkit itself), but available to any solution. |

Choose one mode for the entire solution; the check does not mix modes or fall back between them. Folder-based layering
is recommended for adopters. For a single application, layer projects can sit directly under `src/` rather than under
`src/Library/`. Shared Web is optional. See [Code Organization](https://anarchy-io.github.io/anarchy-toolkit/conventions/code-organization.html) for the layouts.

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
    "applicationFolders": { "src/Api": "Web", "src/Jobs": "Hosting", "src/Ui": "Web" }
  }
}
```

For a single application, set `"libraryRoot": "src"` and map the application's folder (e.g. `{ "src/App": "Web" }`) as an operation choice or saved override. With no configuration, the standard `src/Library` folder baseline is used; mode is never inferred.

For non-interactive setup, choose a preset directly:

```bash
anarchy-toolkit bootstrap Shop --layout single-app --application-folder Shop --save-preferences
anarchy-toolkit scaffold api Orders --layout single-app --application-folder Shop
```

`single-app` puts the layer libraries under `src/` and maps one application folder to `Web` for an API/UI or `Hosting`
for a job. The folder defaults to `App`; choose names such as `Api`, `Job`, `Cli` or the solution name with
`--application-folder`. `monorepo` keeps the standard shared `src/Library` and `src/Api`, `src/Jobs`, `src/Ui` layout.
Both presets accept the regular layering options for custom roots, layers and mappings. See the [preferences guide](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html)
for what belongs in each settings file and how one-run arguments differ from saved defaults.

A job then cannot use web code, and no library can reference an application. Several domains can share one project per layer, side by side in folders (`Core/Orders/`, `Core/Billing/`, `Core/Common/`). See the [Layering convention](https://anarchy-io.github.io/anarchy-toolkit/conventions/layering.html) for the rules and how to bend them.

Prefer placing Anarchy and other third-party dependencies in the shared libraries, with DI modules or wrappers where
useful, so applications can build on a common base. This can pull unused transitive assemblies into an application's
artifacts; the solution developer decides when application-local dependencies are a better trade-off.

## Add applications

```bash
anarchy-toolkit scaffold api Orders
anarchy-toolkit scaffold job InvoiceCleanup
```

Each application is a thin entry point: a `Program`, a `Startup` choosing modules, and settings. The logic goes in the libraries. Describe the settings once in an `AppSettings` class validated on start, and list the keys a developer must provide in `appsettings.json.template`; `make appsettings` asks for them. See [Dependency Injection](https://anarchy-io.github.io/anarchy-toolkit/conventions/dependency-injection.html).

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

Add a runnable project whose name ends in [`MockServer`](https://www.mock-server.com/) (for example `src/MockServer`), referencing `Anarchy.Toolkit.Web.MockServer`, put expectation files under a folder per upstream, and add `IMockServerBuild` to the build:

```json
{ "MockServerExpectations": ["src/MockServer/expectations"] }
```

`make mock_server_start` runs it for local development; `make test_end_to_end_mocked` runs it around the tests. Point the applications' development and test settings at it. The [Mock Server guide](https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html) covers runtime expectations, assertions and recording.

## Bring your company stack

Logging sinks, metrics backend, correlation headers, CodeArtifact or other private feeds, auth: see [Bringing your company stack](https://anarchy-io.github.io/anarchy-toolkit/guides/bringing-your-company-stack.html). Keep that code in your own `Configuration` and `Infrastructure` libraries, in a folder named after the company, so it is easy to find and to replace.

## Add databases and brokers when you need them

Add a `docker/docker-compose.yml` and `IDockerBuild` (`make start`, `stop`, `destroy`), then the persistence or messaging package and its `Testing.*` kit. See [Database Providers](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html) and the [Package Catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/).

## Wire the pipeline

The pull-request pipeline runs one command:

```bash
make pull_request
```

If you deploy with something the toolkit doesn't ship ([CodeDeploy](https://aws.amazon.com/codedeploy/), Lambda zips, a company deploy tool), keep it in a separate release build next to `build/`, with its own make targets. See [Anarchy automation builds](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html) for packages and container images.

## Write down your deviations

Your solution will differ from the conventions somewhere: an exception middleware with your own mapping, a different test layout, a pinned driver. Record each one in an ADR or in `AGENTS.md`, linking the convention you deviate from. Coding agents and new team members then know which rule wins.

## Checklist

- [ ] `make pull_request` passes on a clean clone.
- [ ] `make check_layering` passes, with application folders mapped.
- [ ] Settings validated on start; `appsettings.json.template` lists the local keys.
- [ ] Upstreams mocked; `ShouldAllHaveBeenMatched` fails on unmocked calls.
- [ ] Company specifics isolated in their own folder.
- [ ] Deviations from the conventions recorded.
