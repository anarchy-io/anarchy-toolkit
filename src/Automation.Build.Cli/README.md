# Anarchy.Toolkit.Automation.Build.Cli

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Build Ecosystem](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Automation · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

The `anarchy-toolkit` .NET global tool: bootstrap Anarchy-based solutions and scaffold APIs, jobs and UIs into them.

After the package is published to your configured NuGet feed, install and run it:

```bash
dotnet tool install --global Anarchy.Toolkit.Automation.Build.Cli
anarchy-toolkit
```

Without a command (or with `--help`), the tool shows its usage. Every command starts by printing Anarchy's logo, the same as `scripts/console/banner.sh` (wide on terminals of 116 columns or more, compact from 66, a plain title on narrower ones); it is omitted when the output is redirected.

| Command | What it does |
|---|---|
| `anarchy-toolkit bootstrap` | Guided setup of a new solution. |
| `anarchy-toolkit bootstrap <name> [--output <dir>] [--with-skills] [--layout monorepo\|single-app] [--application-folder <name>] [layout options] [--save-preferences]` | Creates a solution without prompts, in an empty or new directory (default: the current one), optionally with agent skills and explicit preference saving. `monorepo` is the current baseline; `single-app` uses one mapped application folder. |
| `anarchy-toolkit scaffold` | Guided addition of an API, job, UI or test project to the current solution. |
| `anarchy-toolkit scaffold api\|job\|ui <Name> [--layout monorepo\|single-app] [--application-folder <name>] [layout options] [--save-preferences]` | Adds one without prompts; a preset applies to this operation, and saving selected preferences is opt-in. |
| `anarchy-toolkit scaffold library <Name> --layer <Layer> [layout options] [--dry-run] [--save-preferences]` | Adds a library project in a configured layer, referencing the next lower project when present. |
| `anarchy-toolkit scaffold tests [unit\|integration\|endtoend] [--dry-run] [--save-preferences]` | Adds the shared test project for a kind; without a kind, the wizard asks. Honors Fallout `TestKindAliases` and the optional `tests.kindAliases` preference. |
| `anarchy-toolkit scaffold api <Name> [--swagger\|--no-swagger] [--swagger-theme\|--no-swagger-theme] [--save-preferences]` | Adds an API; `--no-swagger` leaves Swagger out, and the theme flags explicitly select the Swagger theme. Saving preferences is opt-in. |
| `anarchy-toolkit config show\|set\|unset\|configure` | Inspect saved versus effective preferences, set or clear a typed JSON override, or interactively edit preferences. |
| `anarchy-toolkit skills install [--output <dir>]` | Installs the toolkit's agent skills into `.agents/skills` of the solution found from the current directory (or `<dir>`). |
| `anarchy-toolkit boilerplate list` | Lists the packaged boilerplate files (Makefile, scripts, settings files, agent skills). |
| `anarchy-toolkit boilerplate show <path>` | Prints a packaged file without installing it. |
| `anarchy-toolkit boilerplate diff [<path>...]` | Prints a unified diff from the solution's copies to the packaged files (`missing:` or `identical:` otherwise); exits with `1` when any file is missing or differs. Folders such as `scripts` select every file below them. `.gitignore` and `.editorconfig` only compare their managed block. |

## Bootstrap

The wizard asks for:

1. the solution name and output directory (default: the solution name under the current directory), which must be empty or not yet exist;
2. whether it will contain one application or several, before asking about the layering mode;
3. for one application, its kind and folder name (default: `App`); for several, which application kinds to start with and a name for each, as many as you like;
4. how `make check_layering` finds each project's layer (see below);
5. the layers, innermost first. A library project is created per layer but `Apps`, each referencing the one below it;
6. whether to install the agent skills (default: no);
7. whether to save selected automation preferences (default: no).

It then shows the plan, including each web application's local ports, and asks for confirmation before writing anything. Every solution also contains an Anarchy build host, Makefile and scripts. Preferences are not saved unless selected in the wizard or requested with `--save-preferences`; a no-save bootstrap remains fully usable with the defaults or repeated explicit layout arguments. Package versions are always managed centrally: the root `Directory.Packages.props` sets `ManagePackageVersionsCentrally` and pins each toolkit package the solution uses, and the projects' `PackageReference`s carry no versions. Existing files are never overwritten; if a failure occurs after generation starts, inspect the destination before retrying.

### Layering

| | By folder (default) | By name prefix |
|---|---|---|
| Libraries | `src/Library/<Layer>/<Layer>.csproj` | `src/Library.<Layer>/Library.<Layer>.csproj` |
| API | `src/Apps/Apis/<Name>`, namespace `<Solution>.Apps.Apis.<Name>` | `src/Apps.Api.<Name>` |
| Job | `src/Apps/Jobs/<Name>`, namespace `<Solution>.Apps.Jobs.<Name>` | `src/Apps.Job.<Name>` |
| UI | `src/Apps/Ui/<Name>`, namespace `<Solution>.Apps.Ui.<Name>` | `src/Apps.Ui.<Name>` |

The default layers are `Core < Infrastructure < Bootstrap < Runtime < Apps`. APIs, jobs and UIs belong to the `Apps` layer, which has no library project: applications reference the `Runtime` library. The effective layout resolves explicit operation arguments, optional saved values in the root `.anarchy-toolkit.json`, then the in-code baseline: folder mode under `src/Library`, standard app folders and standard layers. The mode is never inferred and no preferences file is required. With `--save-preferences`, bootstrap or scaffold persists only deliberately selected preferences; interactive save consent defaults to No. See the [unified schema](https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json) and [preferences guide](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html).

`--layout monorepo` selects that baseline. `--layout single-app` uses folder mode with `libraryRoot` equal to the checked root (`src` by default) and one application folder mapped to the `Apps` layer. The default folder is `App`; pass `--application-folder Api`, `Job`, `Cli`, the solution name, or another single folder name to choose it. A single-app layout can also use prefix mode; in that mode the folder option does not apply. Explicit `--layering-root`, `--layering-layers`, `--layering-library-root`, and `--layering-application-folders` further customize either preset. Every application kind belongs to the `Apps` layer, so a single-app layout accepts an API, job or UI.

### Applications

| Kind | Built on |
|---|---|
| API | `Anarchy.Toolkit.Runtime.Web` (`Anarchy.Toolkit.Runtime.Web.Aws` with `--lambda-api`): REST API with JWT authorization and a `/ping` endpoint |
| Job | `Anarchy.Toolkit.Runtime` (`Anarchy.Toolkit.Runtime.Aws` with `--lambda-event`): job entry point and service |
| UI | `Anarchy.Toolkit.Runtime.Web.Ui`: Blazor UI with cookie-session login and a home page |

Applications are not packable, so `Pack` ignores them. Web applications get local ports from 5000 upwards, two per application, skipping ports already used in legacy `Program.cs` and explicit `EntryPointLocal.cs` files. In folder layering mode startup classes use `<ProjectName>Startup`; prefix mode retains `Startup` or `JobStartup`. Review each application's settings (auth service URLs, client IDs and scopes) before running it.

Every application has a root-level `EntryPointLocal`. The guided wizards offer applicable optional hosts; named commands do not prompt:

```bash
anarchy-toolkit scaffold api Orders --container --lambda-api
anarchy-toolkit scaffold ui Admin --container
anarchy-toolkit scaffold job Reports --job-mode once --lambda-event
anarchy-toolkit scaffold job Processor --job-mode worker
```

Jobs default to a long-running worker; `once` runs the service once and exits. The scheduled Lambda handler invokes the same scoped service and does not start the worker. Deploy it with the generated assembly/type/handler string; scheduling is configured in your cloud infrastructure. For SQS, adapt the handler to accept `SQSEvent`, configure Lambda JSON serialization and an event-source mapping, process each message in its own scope, and return `SQSBatchResponse` with individual failures when partial-batch reporting is enabled. `Anarchy.Toolkit.Infrastructure.Messaging.Aws` offers inbound-message mapping; no SQS dependency is added unless you choose it.

Web containers use `RUNTIME_HOST=container` and `PORT` (default 3000), listen on all interfaces, and expose `/health/instance`. Their forwarded-header settings trust the upstream network; restrict access to trusted proxies or customize the configuration. The API Lambda targets API Gateway REST/HTTP API payload v1. Interactive-server Blazor needs persistent connections, so UI Lambda is not offered: deploy UIs to AWS EC2 or ECS, or Azure virtual machines or Azure Container Apps instead. Configure WebSockets and connection affinity or an appropriate shared backplane when scaling. `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` control application configuration independently of `RUNTIME_HOST`; set them explicitly for local development and CI.

### Scaffold libraries and test suites

Add a library project to an existing layer or scaffold the shared test project for one test kind:

```bash
anarchy-toolkit scaffold library Orders --layer Core
anarchy-toolkit scaffold library Postgres --layer Infrastructure --dry-run
anarchy-toolkit scaffold tests unit
anarchy-toolkit scaffold tests endtoend --layout monorepo --save-preferences
```

Folder mode places libraries below the effective `libraryRoot`; prefix mode uses `<Layer>.<Name>` under the checked
root. The new library references the immediately lower layer's project if it exists. Test projects go below the
effective test root (`TestsRoot`, then saved `build.testsRoot`, then `tests`) and use the canonical kind name or its
configured `TestKindAliases` name. The generated project inherits shared settings from `Directory.Build.props` in that
test root. `--dry-run` reports the project and solution changes without writing files or preferences. `--save-preferences`
is opt-in and persists only layout values explicitly selected for the operation.

## Scaffold

Run `anarchy-toolkit scaffold` anywhere inside a solution created by `bootstrap` (or any solution whose `.fallout/parameters.json` names its solution file). It uses explicit arguments, saved preferences and then defaults; it does not require `.anarchy-toolkit.json` or infer a layout. It refuses names already used by a project and adds the new application to the solution.

It follows the solution's package management. When the nearest `Directory.Packages.props` above the new project enables central package management (in that file or in `Directory.Build.props`), the package version is added there as a `PackageVersion`, unless the package is already pinned, in which case the existing version is kept, and the project's reference is unversioned. Otherwise the reference carries the version.

Generated APIs include Swagger: they reference `Anarchy.Toolkit.Runtime.Web.Swagger` (pinned in `Directory.Packages.props` under central package management) and call `AddToolkitSwagger` in their startup. The `scaffolding.swagger` preference controls the default; `--no-swagger` (or `--swagger`) overrides it for one scaffold operation, and the wizard asks "Include Swagger?". With Swagger, generated APIs use the toolkit's default Swagger UI theme (see the [Runtime.Web.Swagger package README](../Runtime.Web.Swagger/README.md#swagger-ui-theme)). The `scaffolding.swaggerTheme` preference controls the default. `--swagger-theme` and `--no-swagger-theme` explicitly enable or disable it for one scaffold operation, overriding a saved choice; they cannot be combined with `--no-swagger`.

### Preference commands

```bash
anarchy-toolkit config show
anarchy-toolkit config set build.configuration '"Release"'
anarchy-toolkit config unset build.configuration
anarchy-toolkit config configure
```

`set` takes a `section.property` and a JSON value, merges only that value and preserves unrelated preferences; `unset`
removes the saved override. Both validate before an atomic write. `show` lists effective values with their source
(`saved value` or `in-code default`) without creating a file. It cannot show parameters from a separate build invocation;
the Fallout build's `ShowAnarchyPreferences` target labels active layering parameters when they are passed to it. The interactive wizard previews and asks
before writing. There is no automatic config save. See the [preference inventory and build/Make equivalents](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html).

## Boilerplate

The `boilerplate` commands print plain text, without the banner, so scripts and agents can read the packaged files of this tool's version before deciding what to install: `make boilerplate DRY_RUN=true` then shows what an install would create, keep, update or refuse, and `make boilerplate_diff` does the same comparison as `boilerplate diff` through the solution's build.

## Agent skills

`skills install` (and `bootstrap --with-skills`) copies the toolkit's [Agent Skills](https://agentskills.io/) to `.agents/skills`: `anarchy`, the entry point with this tool's commands and the rules; `anarchy-adopt`, for first-time adoption and initial repository setup; `anarchy-compose`, for modules, settings, health reports and workers; `anarchy-conventions`, the recommended coding conventions with the values and decisions behind them and how to opt out; and `anarchy-develop`, `anarchy-environment`, `anarchy-release` and `anarchy-maintenance`. Existing files are never overwritten. See [Agent skills](../Automation.Build/README.md#agent-skills) for keeping them in step with the solution's Makefiles.

## Requirements

The .NET 10 SDK is required to generate and build solutions. The generated projects reference the `Anarchy.Toolkit.Automation.Build`, `Anarchy.Toolkit.Runtime`, `Anarchy.Toolkit.Runtime.Web`, `Anarchy.Toolkit.Runtime.Web.Ui` and, when chosen, `Anarchy.Toolkit.Runtime.Aws`, `Anarchy.Toolkit.Runtime.Web.Aws`, `Anarchy.Toolkit.Runtime.Web.Swagger` and test-kit versions this tool was built with, which must be available on a configured NuGet feed. The generated `nuget.config` uses nuget.org by default; adjust it if using a private feed.

When releasing the tool, publish those packages first and wait for them to be available on the feed. A .NET tool declares no NuGet dependencies, so package dependency ordering alone does not enforce this.

To try the build (Bash and Make required):

```bash
cd MyApp
make compile
make check_layering
```

The bootstrapping and scaffolding live in [`Anarchy.Toolkit.Automation`](../Automation/README.md); see the [automation build package README](../Automation.Build/README.md) for the build targets.

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Spectre.Console](https://www.nuget.org/packages/Spectre.Console) | The wizards, prompts and banner | No: a .NET tool, not a library |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
