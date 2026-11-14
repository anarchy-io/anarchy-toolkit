# Anarchy.Toolkit.Build.Cli.Tool

The `anarchy-toolkit` .NET global tool: bootstrap Anarchy-based solutions and scaffold APIs, jobs and UIs into them.

After the package is published to your configured NuGet feed, install and run it:

```bash
dotnet tool install --global Anarchy.Toolkit.Build.Cli.Tool
anarchy-toolkit
```

Without a command (or with `--help`), the tool shows its usage. Every command starts by printing Anarchy's logo, the same as `scripts/console/banner.sh` (wide on terminals of 116 columns or more, compact otherwise); it is omitted when the output is redirected.

| Command | What it does |
|---|---|
| `anarchy-toolkit bootstrap` | Guided setup of a new solution. |
| `anarchy-toolkit bootstrap <name> [--output <dir>]` | Creates the baseline solution without prompts, in an empty or new directory (default: the current one). |
| `anarchy-toolkit scaffold` | Guided addition of an API, job or UI to the current solution. |
| `anarchy-toolkit scaffold api\|job\|ui <Name>` | Adds one without prompts. |

## Bootstrap

The wizard asks for:

1. the solution name;
2. the output directory (default: the solution name under the current directory), which must be empty or not yet exist;
3. how `make check_layering` finds each project's layer (see below);
4. the layers, innermost first (default: `Core, Infrastructure, Configuration, Hosting, Web`). A library project is created per layer, each referencing the one below it;
5. which kinds of application to start with, then a name for each one, as many as you like.

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

### 2.0.0

- feat(Build.Fallout): replace AWS release flows with Docker and container builds

### 1.0.0

- Introduced the `anarchy-toolkit` tool with the `bootstrap` and `scaffold` commands, configurable layering, and API, job and UI templates.
