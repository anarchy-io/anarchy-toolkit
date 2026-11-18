# Anarchy.Toolkit.Build.Cli.Tool

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Build Ecosystem](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Build · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

The `anarchy-toolkit` .NET global tool: bootstrap Anarchy-based solutions and scaffold APIs, jobs and UIs into them.

After the package is published to your configured NuGet feed, install and run it:

```bash
dotnet tool install --global Anarchy.Toolkit.Build.Cli.Tool
anarchy-toolkit
```

Without a command (or with `--help`), the tool shows its usage. Every command starts by printing Anarchy's logo, the same as `scripts/console/banner.sh` (wide on terminals of 116 columns or more, compact from 66, a plain title on narrower ones); it is omitted when the output is redirected.

| Command | What it does |
|---|---|
| `anarchy-toolkit bootstrap` | Guided setup of a new solution. |
| `anarchy-toolkit bootstrap <name> [--output <dir>] [--with-skills]` | Creates the baseline solution without prompts, in an empty or new directory (default: the current one), optionally with the agent skills. |
| `anarchy-toolkit scaffold` | Guided addition of an API, job or UI to the current solution. |
| `anarchy-toolkit scaffold api\|job\|ui <Name>` | Adds one without prompts. |
| `anarchy-toolkit scaffold api <Name> --no-swagger-theme` | Adds an API whose Swagger UI keeps its own look instead of the toolkit's default theme. |
| `anarchy-toolkit skills install [--output <dir>]` | Installs the toolkit's agent skills into `.agents/skills` of the solution found from the current directory (or `<dir>`). |
| `anarchy-toolkit boilerplate list` | Lists the packaged boilerplate files (Makefile, scripts, settings files, agent skills). |
| `anarchy-toolkit boilerplate show <path>` | Prints a packaged file without installing it. |
| `anarchy-toolkit boilerplate diff [<path>...]` | Prints a unified diff from the solution's copies to the packaged files (`missing:` or `identical:` otherwise); exits with `1` when any file is missing or differs. Folders such as `scripts` select every file below them. `.gitignore` and `.editorconfig` only compare their managed block. |

## Bootstrap

The wizard asks for:

1. the solution name;
2. the output directory (default: the solution name under the current directory), which must be empty or not yet exist;
3. how `make check_layering` finds each project's layer (see below);
4. the layers, innermost first (default: `Core, Infrastructure, Configuration, Hosting, Web`). A library project is created per layer, each referencing the one below it;
5. which kinds of application to start with, then a name for each one, as many as you like;
6. whether to install the agent skills (default: no).

It then shows the plan, including each web application's local ports, and asks for confirmation before writing anything. Every solution also contains a Fallout build host, Makefile, scripts and configuration. Package versions are always managed centrally: the root `Directory.Packages.props` sets `ManagePackageVersionsCentrally` and pins each toolkit package the solution uses, and the projects' `PackageReference`s carry no versions. Existing files are never overwritten; if a failure occurs after generation starts, inspect the destination before retrying.

### Layering

| | By folder (default) | By name prefix |
|---|---|---|
| Libraries | `src/Library/<Layer>/<Layer>.csproj` | `src/<Layer>/<Layer>.csproj` |
| API | `src/Api/<Name>`, namespace `Api.<Name>` | `src/Web.Api.<Name>` |
| Job | `src/Jobs/<Name>`, namespace `Jobs.<Name>` | `src/Hosting.Job.<Name>` |
| UI | `src/Ui/<Name>`, namespace `Ui.<Name>` | `src/Web.Ui.<Name>` |

APIs and UIs belong to the `Web` layer and jobs to the `Hosting` layer; each references its layer's library. The layering settings are written to `.fallout/parameters.json` (`LayeringLayers`, `LayeringByFolder`, `LayeringRoot` and, by folder, `LayeringApplicationFolders`), where `CheckLayering` and `anarchy-toolkit scaffold` read them.

### Applications

| Kind | Built on |
|---|---|
| API | `Anarchy.Toolkit.Web`: REST API with JWT authorization and a `/ping` endpoint |
| Job | `Anarchy.Toolkit.Hosting`: job entry point (e.g. an AWS Lambda handler) and service |
| UI | `Anarchy.Toolkit.Web.Ui`: Blazor UI with cookie-session login and a home page |

Applications are not packable, so `Pack` ignores them. Web applications get local ports from 5000 upwards, two per application, skipping ports already used in the solution. Review each application's settings (auth service URLs, client IDs and scopes) before running it.

## Scaffold

Run `anarchy-toolkit scaffold` anywhere inside a solution created by `bootstrap` (or any solution whose `.fallout/parameters.json` names its solution file). It places the application according to the solution's layering settings, refuses names already used by a project, and adds it to the solution.

It follows the solution's package management. When the nearest `Directory.Packages.props` above the new project enables central package management (in that file or in `Directory.Build.props`), the package version is added there as a `PackageVersion`, unless the package is already pinned, in which case the existing version is kept, and the project's reference is unversioned. Otherwise the reference carries the version.

Generated APIs use the toolkit's default Swagger UI theme (see the [Web package README](../Web/README.md#swagger-ui-theme)). With `--no-swagger-theme`, the startup disables it with `.Configure<SwaggerThemeOptions>(theme => theme.Enabled = false)`; remove that line to turn it back on.

## Boilerplate

The `boilerplate` commands print plain text, without the banner, so scripts and agents can read the packaged files of this tool's version before deciding what to install: `make boilerplate DRY_RUN=true` then shows what an install would create, keep, update or refuse, and `make boilerplate_diff` does the same comparison as `boilerplate diff` through the solution's build.

## Agent skills

`skills install` (and `bootstrap --with-skills`) copies the toolkit's [Agent Skills](https://agentskills.io/) to `.agents/skills`: `anarchy`, the entry point with this tool's commands and the rules; `anarchy-adopt`, for adopting or upgrading the toolkit (including migrating tests, mocks and DI); `anarchy-compose`, for modules, settings, health reports and workers; and `anarchy-develop`, `anarchy-environment`, `anarchy-release` and `anarchy-maintenance`. Existing files are never overwritten. See [Agent skills](../Build.Fallout/README.md#agent-skills) for keeping them in step with the solution's Makefiles.

## Requirements

The .NET 10 SDK is required to generate and build solutions. The generated projects reference the `Anarchy.Toolkit.Build.Fallout`, `Anarchy.Toolkit.Web`, `Anarchy.Toolkit.Web.Ui` and `Anarchy.Toolkit.Hosting` versions this tool was built with, which must be available on a configured NuGet feed. The generated `nuget.config` uses nuget.org by default; adjust it if using a private feed.

When releasing the tool, publish those packages first and wait for them to be available on the feed. A .NET tool declares no NuGet dependencies, so package dependency ordering alone does not enforce this.

To try the build (Bash and Make required):

```bash
cd MyApp
make compile
make check_layering
```

The bootstrapping and scaffolding live in [`Anarchy.Toolkit.Build.Cli`](../Build.Cli/README.md); see the [Fallout package README](../Build.Fallout/README.md) for the build targets.

## Releases

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
