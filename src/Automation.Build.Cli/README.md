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
| `anarchy-toolkit scaffold api <Name> [--swagger-theme\|--no-swagger-theme] [--save-preferences]` | Adds an API; either flag explicitly selects the Swagger theme. Saving preferences is opt-in. |
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
5. the layers, innermost first. A library project is created per layer, each referencing the one below it;
6. whether to install the agent skills (default: no);
7. whether to save selected automation preferences (default: no).

It then shows the plan, including each web application's local ports, and asks for confirmation before writing anything. Every solution also contains an Anarchy build host, Makefile and scripts. Preferences are not saved unless selected in the wizard or requested with `--save-preferences`; a no-save bootstrap remains fully usable with the defaults or repeated explicit layout arguments. Package versions are always managed centrally: the root `Directory.Packages.props` sets `ManagePackageVersionsCentrally` and pins each toolkit package the solution uses, and the projects' `PackageReference`s carry no versions. Existing files are never overwritten; if a failure occurs after generation starts, inspect the destination before retrying.

### Layering

| | By folder (default) | By name prefix |
|---|---|---|
| Libraries | `src/Library/<Layer>/<Layer>.csproj` | `src/<Layer>/<Layer>.csproj` |
| API | `src/Api/<Name>`, namespace `Api.<Name>` | `src/Web.Api.<Name>` |
| Job | `src/Jobs/<Name>`, namespace `Jobs.<Name>` | `src/Hosting.Job.<Name>` |
| UI | `src/Ui/<Name>`, namespace `Ui.<Name>` | `src/Web.Ui.<Name>` |

APIs and UIs belong to the `Web` layer and jobs to the `Hosting` layer; each references its layer's library. The effective layout resolves explicit operation arguments, optional saved values in the root `.anarchy-toolkit.json`, then the in-code baseline: folder mode under `src/Library`, standard app folders and standard layers. The mode is never inferred and no preferences file is required. With `--save-preferences`, bootstrap or scaffold persists only deliberately selected preferences; interactive save consent defaults to No. See the [unified schema](https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json) and [preferences guide](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html).

`--layout monorepo` selects that baseline. `--layout single-app` uses folder mode with `libraryRoot` equal to the checked root (`src` by default) and one application folder mapped to the application's outer layer: `Web` for an API/UI or `Hosting` for a job. The default folder is `App`; pass `--application-folder Api`, `Job`, `Cli`, the solution name, or another single folder name to choose it. A single-app layout can also use prefix mode; in that mode the folder option does not apply. Explicit `--layering-root`, `--layering-layers`, `--layering-library-root`, and `--layering-application-folders` further customize either preset. On non-interactive bootstrap without an application kind, single-app starts with the `Web` layer; `scaffold` selects the correct outer layer for its API, job or UI.

### Applications

| Kind | Built on |
|---|---|
| API | `Anarchy.Toolkit.Web`: REST API with JWT authorization and a `/ping` endpoint |
| Job | `Anarchy.Toolkit.Hosting`: job entry point (e.g. an AWS Lambda handler) and service |
| UI | `Anarchy.Toolkit.Web.Ui`: Blazor UI with cookie-session login and a home page |

Applications are not packable, so `Pack` ignores them. Web applications get local ports from 5000 upwards, two per application, skipping ports already used in the solution. Review each application's settings (auth service URLs, client IDs and scopes) before running it.

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

Generated APIs use the toolkit's default Swagger UI theme (see the [Web package README](../Web/README.md#swagger-ui-theme)). The `scaffolding.swaggerTheme` preference controls the default. `--swagger-theme` and `--no-swagger-theme` explicitly enable or disable it for one scaffold operation, overriding a saved choice.

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

The .NET 10 SDK is required to generate and build solutions. The generated projects reference the `Anarchy.Toolkit.Automation.Build`, `Anarchy.Toolkit.Web`, `Anarchy.Toolkit.Web.Ui` and `Anarchy.Toolkit.Hosting` versions this tool was built with, which must be available on a configured NuGet feed. The generated `nuget.config` uses nuget.org by default; adjust it if using a private feed.

When releasing the tool, publish those packages first and wait for them to be available on the feed. A .NET tool declares no NuGet dependencies, so package dependency ordering alone does not enforce this.

To try the build (Bash and Make required):

```bash
cd MyApp
make compile
make check_layering
```

The bootstrapping and scaffolding live in [`Anarchy.Toolkit.Automation`](../Automation/README.md); see the [automation build package README](../Automation.Build/README.md) for the build targets.

## Releases

### 1.0.0

- New package identity, renamed from `Anarchy.Toolkit.Build.Cli.Tool`; versions below are historical releases under that package ID.
- Add optional root preferences and `config show/set/unset/configure`; bootstrap/scaffold save selected preferences only when explicitly requested or after wizard consent (default No).
- Establish adopter organization conventions and allow non-interactive bootstrap to select folder-based or prefix-based layering.
- Resolve layering from explicit arguments, saved overrides, then the documented folder baseline without inference or a required config file; support both positive and negative Swagger-theme overrides.
- Scaffold APIs and jobs with provider-neutral application telemetry.

### Historical releases under `Anarchy.Toolkit.Build.Cli.Tool`

### 3.3.0

- docs(conventions): separate adopter recommendations from contributor rules
- feat(Build.Cli): ship the conventions and onboarding guides in the agent skills

### 3.2.0

- fix(Build.Cli): pack the boilerplate at its source paths
- feat(Build.Cli): ship a generic .gitignore synced through a managed block
- feat(Build.Cli): show, diff and dry-run the boilerplate before installing it
- feat(Build.Cli): keep the packaged .editorconfig in a managed block
- docs(mock-server): add a guide covering recording and migrating from MockServer
- feat(Build.Cli): ship adoption and runtime composition skills

### 3.1.0

- feat(Build.Cli): add opt-in agent skills to the boilerplate
- feat(Build.Fallout): validate and sync agent skills
- feat(Build.Cli.Tool): install agent skills from the CLI
- fix(Build.Cli): print a plain title on narrow terminals
- fix(Build.Cli): keep shell completion working for including Makefiles

### 3.0.0

- refactor(Web.Ui)!: make the cookie session an opt-in add-on
- feat(Build.Fallout): run the mock server with or without its UI

### 2.6.2

- fix(Web.Ui): serve static web assets of unpublished builds outside Development

### 2.6.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata
- fix(mock-server): default to port 6001, which browsers allow

### 2.6.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1
- feat(Web): serve OpenAPI 3.2 and theme Swagger UI with a light and dark Anarchy theme
- feat(Web.Ui): align the UI theme with the documentation and add dark mode
- feat(Build.Cli): scaffold APIs without the Swagger theme and UIs with the logo and theme toggle
- feat(Build.Cli.Tool): add the --no-swagger-theme flag to scaffold api

### 2.3.1

- docs: expand XML docblocks across all projects for the DocFX API reference

### 2.3.0

- feat(Build.Fallout): add IApiReferenceBuild generating a DocFX API reference from the compiled assemblies

### 2.2.0

- Generated solutions reference `Anarchy.Toolkit.Build.Fallout` 3.2.0 and `Anarchy.Toolkit.Web`/`Anarchy.Toolkit.Web.Ui` 1.0.7, and get the `Anarchy.Toolkit.Build.Cli` 1.2.1 SDK installation and `DOTNET_EXE` fixes in their scripts.

### 2.1.0

- Prints its banner through `Anarchy.Toolkit.Build.Cli` 1.2.0's `ConsoleBannerOptions` (still always Anarchy's), and generated solutions reference `Anarchy.Toolkit.Build.Fallout` 3.1.0.

### 2.0.0

- feat(Build.Fallout): replace AWS release flows with Docker and container builds

### 1.0.0

- Introduced the `anarchy-toolkit` tool with the `bootstrap` and `scaffold` commands, configurable layering, and API, job and UI templates.
- Add `monorepo` and `single-app` layout presets, a one-or-several-applications bootstrap choice, and effective preference-source reporting.
- Add `scaffold library` and `scaffold tests` with dry-run previews, opt-in layout saving and configured test-kind aliases.
