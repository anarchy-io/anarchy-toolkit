# Anarchy.Toolkit.Automation.Build

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Build Ecosystem](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Automation · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Reusable Anarchy build components for .NET repositories, powered by [Fallout Build](https://fallout.build/).

```bash
dotnet add build/_build.csproj package Anarchy.Toolkit.Automation.Build
```

Implement the components appropriate to your repository on an `AnarchyBuild` class. For example, a package-only build can implement `INuGetBuild`, `ILayeringBuild`, `IDependencyMapBuild`, `IDependencyUpgradeBuild` and `IGitHooksBuild`, with an empty `Artifacts` collection. Use `IBoilerplateBuild` for scaffolding, add `IApiReferenceBuild` for a DocFX API reference site, or add the container (`IContainerBuild`, `IDockerBuild`) components as needed. External deployment and publication targets require their respective tools and credentials.

### Fallout types in the public API

The build components intentionally expose a small set of Fallout Build types because consumers compose Fallout targets
and operate on Fallout's loaded solution model: `Target`, `AbsolutePath`, `Solution`, `Project`, `GitRepository`, and
`DotNetVerbosity`, together with Fallout's `[Parameter]` and `[Solution]` attributes. These types are part of the
Automation.Build contract, not accidental implementation details. Toolkit-owned concepts such as `SolutionLayout`,
`Artifact`, and preference values use toolkit types; adapters or wrappers would obscure the Fallout operations the
consumer needs to customize.

```csharp
using Anarchy.Toolkit.Automation.Build.Component;
using Anarchy.Toolkit.Automation.Build.Core.Entity.Build;
using Anarchy.Toolkit.Automation.Build.Extensions.Fallout.Common;
class Build : AnarchyBuild, INuGetBuild, ILayeringBuild, IDependencyMapBuild,
              IDependencyUpgradeBuild, IGitHooksBuild
{
    public IReadOnlyCollection<Artifact> Artifacts => [];
    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
}
```

Create Fallout's `.fallout/parameters.json` with the solution path, then run `dotnet run --project build/_build.csproj -- --help` to see targets and parameters. Layering and other toolkit preferences are optional in the solution-root `.anarchy-toolkit.json` (schema: [anarchy-toolkit.schema.json](https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json)); without overrides, the effective layering baseline is folder mode under `src/Library` with standard application folders and layers. Existing Fallout parameter resolution takes precedence over saved preferences, then in-code defaults. A missing file or layout mode is valid and no mode is inferred. The included scaffolding target writes its engine-owned solution setting to `.fallout/parameters.json`; it will not overwrite existing files.

See the [Anarchy automation builds guide](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html) for component details and package publishing.

## Local containers

`IDockerBuild` manages a local environment on a Docker Compose file (default: `docker/docker-compose.yml`, project named after the repository directory). It uses `docker compose` when available, otherwise the legacy `docker-compose`, and passes the compose file's directory as `DOCKER_ROOT`. The boilerplate's `docker.mk` exposes each target to Make:

| Target | Make | What it does |
|---|---|---|
| `DockerInfo` | `docker_info` | Docker engine and Compose versions, the Compose command, and the running containers |
| `DockerStopAll` | `docker_stopall` | Stops every container running on the host |
| `DockerLogs` | `container_logs` | Prints the environment's logs, of `DockerContainer` if given |
| `DockerStart` | `start` | `up --detach --quiet-pull --remove-orphans`, then waits for each `DockerWaitFor` container (healthy, with `DockerWaitForHealthy`) |
| `DockerStop`, `DockerRestart`, `DockerDestroy` | `stop`, `restart`, `destroy` | Stop, restart (`DockerContainer` only, if given) or remove the environment, including its volumes |
| `DockerWaitRunning` | `wait_running_container` | Waits up to `DockerTimeout` seconds (default 60) for `DockerContainer` to be running, then, if `DockerReadinessCheck` is overridden, up to `DockerTimeout` more for it to pass |
| `DockerWaitHealthy` | `wait_healthy_container` | Waits up to `DockerStartTimeout` seconds (default 180) for it to be running, then up to `DockerTimeout` for its health check to pass; fails at once if it turns unhealthy or has no health check |

The waits show a progress bar on interactive terminals and plain lines otherwise (CI, redirected output).

### Prepare local services for tests

Implement `ITestEnvironmentBuild` instead of `IDockerBuild` on a build that uses `IStandardBuild` (including builds
that get it through `INuGetBuild` or `IMockServerBuild`). Integration and end-to-end targets, including their mock-server
variants, will then start Compose and wait for the services configured through `DockerStart`; unit tests will not.
For example, set these preferences in `.anarchy-toolkit.json` and define a health check on the Compose service:

```json
{ "docker": { "waitFor": ["postgres"], "waitForHealthy": true } }
```

Use the name that Docker can inspect (for example an explicit Compose `container_name`). Set `docker.timeout` and
`docker.startTimeout` if the defaults are too short. `make test_integration` and `make test_end_to_end` prepare the
environment automatically; `make prepare_test_environment` (`PrepareTestEnvironment`) does it without running tests.
`make start` remains available for Compose alone. Nothing stops the environment or removes data after a test run.
For host-run tests, publish the required ports and configure the tests to connect through those host ports.

### Run repository-owned migrations with Flyway

Add `IFlywayBuild` if the repository owns the migration history it uses in delivery as well as local development.
It is not necessary for throwaway test databases initialized by their container images, or for partial copies of a
schema owned elsewhere. `IFlywayBuild` exposes both targets below. With `ITestEnvironmentBuild` also present, the
test preparation chain starts Compose, waits for the configured database containers, and runs `FlywayMigrate` before
tests. Neither component is required for builds that do not need this workflow.

| Target | Make | What it does |
|---|---|---|
| `FlywayMigrate` | `flyway_migrate` | Applies pending migrations. Also starts and waits for Compose if `ITestEnvironmentBuild` is present. |
| `FlywayInfo` | `flyway_info` | Shows migration status without starting Compose. The database must already be reachable. |

Configure the build parameters in `.fallout/parameters.json`, or pass them to Fallout for an individual run:

| Fallout parameter | Make override | Value |
|---|---|---|
| `FlywayImage` | `FLYWAY_IMAGE` | Flyway CLI image tag or digest; defaults to `redgate/flyway:11.8.2`. |
| `FlywayConfigFiles` | `FLYWAY_CONFIG_FILES` | One or more `.conf` or `.toml` paths, in loading order. Use the same format for all files in a run. Keep credentials out of tracked configuration. |
| `FlywayLocations` | `FLYWAY_LOCATIONS` | Migration directories. If omitted, Flyway's configured or image-default locations apply. |
| `FlywayNetwork` | `FLYWAY_NETWORK` | Docker network for the one-shot container. Set it to the database's Compose network (often `<project>_default`) to use the service hostname in the JDBC URL. If omitted, Docker's default network applies. |

```sh
make flyway_info FLYWAY_CONFIG_FILES=docker/flyway-local.conf FLYWAY_LOCATIONS=db/migrations FLYWAY_NETWORK=myapp_default
```

Paths are relative to the repository root or absolute. The build mounts configured files and migration directories
read-only and passes their container paths to Flyway. The CLI container is removed when it exits. A nonzero exit
fails the target and blocks any dependent tests.

## Container images

`IContainerBuild` publishes a runnable app, builds its image from `docker/Dockerfile` by default, then pushes it to the
configured `ContainerRegistry`. The artifact directory defaults to `artifacts/Container/<app>`; use any registry the
Docker client supports. Authenticate before pushing: for ECR, run `aws ecr get-login-password --region <region> | docker
login --username AWS --password-stdin <account>.dkr.ecr.<region>.amazonaws.com`; for GHCR, use
`echo "$GHCR_TOKEN" | docker login ghcr.io -u <user> --password-stdin`; ACR uses `az acr login --name <registry>`;
Docker Hub uses `docker login`. The toolkit does not log in or manage registry credentials.

The build targets are `ContainerPublishAppArtifact`, `ContainerImageBuild` and `ContainerImagePush`. Their Make wrappers
are `container_publish_app_artifact`, `container_image_build` and `container_image_push` (each takes `APP_NAME`).

For containers without a health check, override `DockerReadinessCheck` in the build to add your own; it is called once a second with the container's name until it returns true:

```csharp
Func<string, bool>? IDockerBuild.DockerReadinessCheck => container =>
{
    try { using var client = new TcpClient("localhost", 5432); return true; }
    catch (SocketException) { return false; }
};
```

## Starting from an empty folder

For a guided setup, including APIs, jobs and UIs, install the `Anarchy.Toolkit.Automation.Build.Cli` global tool and run `anarchy-toolkit bootstrap`. See the [tool README](../Automation.Build.Cli/README.md). To set up the build host manually instead:

With the .NET 10 SDK, Bash and Make installed, create a build host first:

```bash
mkdir -p build .fallout
dotnet new console --framework net10.0 --name _build --output build
dotnet add build/_build.csproj package Anarchy.Toolkit.Automation.Build
printf '{}\n' > .fallout/parameters.json
```

Use the example above in `build/Program.cs`, adding `IBoilerplateBuild` to the class. Then generate a starter solution and try the build:

```bash
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-solution-name MyApp
make compile
make check_layering
```

By default, scaffolding creates a solution containing the build and a starter `src/Core` project, along with `tests/`, `samples/`, `docs/`, `Makefile`, scripts and configuration files. It does not create an automation preferences file unless explicitly asked to save preferences. Review the supplied .NET SDK and test-runner settings and enable or adapt the optional Make targets for your build components and tools.

### Choosing what to install

Use `--boilerplate-items` for a comma-separated allowlist and `--boilerplate-exclude` to remove items. Names are case-insensitive; omit the allowlist to select everything.

| Item | Effect |
|---|---|
| `solution` | Create the named `.sln`, add the existing build and selected Core project, and set `Solution` in `.fallout/parameters.json`. Requires `--boilerplate-solution-name`. |
| `core` | Create `src/Core/Core.csproj`. Add it to a solution yourself if `solution` is excluded. |
| `tests`, `samples`, `docs` | Create the directory and its `.gitkeep`. |
| `Makefile`, `.gitignore`, `.editorconfig`, `global.json`, `nuget.config` | Install the corresponding packaged file. |
| `Directory.Build.props` | Create the SQL resource embedding rule if the file is absent. |
| `scripts` | Install all scripts; individual paths such as `scripts/build.anarchy.sh`, `scripts/build.anarchy.ps1` or `scripts/bootstrap` can also be selected or excluded. |
| `skills` | Install the toolkit's agent skills into `.agents/skills` (see [Agent skills](#agent-skills)). Opt-in: `all` leaves them out. |

`all` selects every item except `skills`, and `none` selects nothing. Unknown names fail before any files are written. When selecting individual scripts, include their helpers: the launcher needs `scripts/shared/colors.env`, `scripts/console/subtitle.sh` and, unless environment reloading is disabled, `scripts/bootstrap/shell-profile-path.sh`.

Existing `.gitignore` and `.editorconfig` files are untouched unless `--append-existing` (or `--apend-existing`) is supplied. The packaged content then goes into a managed block between `# >>> anarchy-toolkit` and `# <<< anarchy-toolkit` lines: a later run replaces that block with the current packaged settings, and never touches the lines outside it, where the repository keeps its own entries. In `.gitignore`, the block leaves out the entries the file already has. Whenever it writes a `.gitignore`, the target warns about files Git tracks that the new rules ignore, so you can untrack them or add a `!` exception below the block. In `.editorconfig`, override a setting by repeating its section below the block (for example `[*.cs]` then `dotnet_diagnostic.S8970.severity = none`): later entries win, so the toolkit owns the block and the repository owns its exceptions. Review the result because later rules can change earlier ones. The same option adds the packaged `test` section (`"runner": "Microsoft.Testing.Platform"`, which `dotnet test` needs to run xUnit.net v3 projects) to an existing `global.json` that does not have one; the rest of the file is kept, and a file with comments is left alone. Existing `nuget.config` and `Directory.Build.props` are always preserved, as is `global.json` otherwise. The target refuses to overwrite a solution, selected script, Makefile, starter project, `.gitkeep` or existing `Solution` parameter. If an operation fails partway through, inspect the output before retrying.

To look before writing, `--boilerplate-dry-run` (`make boilerplate DRY_RUN=true`) only logs what the target would create, keep or update (with the managed block's diff) and which files conflict. `DiffBoilerplate` (`make boilerplate_diff ITEMS=Makefile,scripts`) logs a unified diff from each of the solution's copies to the packaged file, for all items but the skills when `--boilerplate-items` is not set.

## Agent skills

The toolkit ships eight [Agent Skills](https://agentskills.io/) so coding agents run the solution's operations the way the toolkit intends: `anarchy` (the entry point: CLI, rules and which skill to load), `anarchy-adopt` (first-time adoption and initial repository setup), `anarchy-compose` (modules, settings, health reports, workers), `anarchy-conventions` (the recommended coding conventions and the decisions behind them), `anarchy-develop`, `anarchy-environment`, `anarchy-release` and `anarchy-maintenance`. They are opt-in: `make install_skills` (or `make boilerplate ITEMS=skills`, or `anarchy-toolkit skills install`) copies them to `.agents/skills`, never overwriting existing files.

A skill is free text (when to use the commands, how to combine them, what to avoid) plus a commands table generated between `<!-- anarchy:commands:begin … -->` and `<!-- anarchy:commands:end -->` from the Makefiles' `#?`, `#+` and `#!` comments. Make targets declare their skill with a `#@` comment, which `make help COMMAND=<target>` also shows:

```make
#@ shop-billing              # before the first target: every documented target of the file
invoice: #? Issues the pending invoices
#@ shop-admin                # right after a target: that target only; '#@ -' exempts it
```

A bare `#@` names the skill `<prefix>-<file>`. `IAgentSkillsBuild` keeps the skills in step:

To add a skill for this solution's own targets, document them with `#?` (and parameters with `#+ NAME|type|description`),
mark a `.mk` file with `#@ <skill-name>` or mark an individual target just below it, then run `make skills_sync`.
Replace the generated TODO description with when and how to use the commands, and run `make skills_validate` to check
the skill. `#@ -` exempts a target.

| Target | Make | What it does |
|---|---|---|
| `SyncAgentSkills` | `skills_sync [PREFIX=…]` | Creates each missing skill from a template and rewrites the commands block of every skill, keeping the rest of the file |
| `ValidateAgentSkills` | `skills_validate [PREFIX=…]` | Fails when a required skill is missing or invalid, still has the template's `TODO` description, lost its `## When to use` or `## Commands` section, or lists outdated commands |

`--agent-skills-prefix` defaults to the solution's name, and `--agent-skills-directories` to `.agents/skills` (new skills are created in the first). These can also be saved under `agentSkills` in `.anarchy-toolkit.json`. `ValidateAgentSkills` is not part of `PullRequest`; run it as its own CI step.

### Automation preferences

Fallout resolves command-line, environment and `parameters.json` values using its existing precedence before consulting
the optional root `.anarchy-toolkit.json`, then the in-code default. To inspect or deliberately edit saved overrides,
use `ShowAnarchyPreferences`, `SetAnarchyPreference --PreferenceKey section.property --PreferenceValue '<JSON>'`,
`UnsetAnarchyPreference --PreferenceKey section.property` or `ConfigureAnarchyPreferences`; Make wrappers are
`config_show`, `config_set KEY=section.property VALUE='<JSON>'`, `config_unset KEY=section.property` and
`config_configure`. Edits merge only requested keys, validate the resulting preferences and write atomically. Normal
build execution never writes preferences. See the [complete inventory and defaults](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html).

## Mock server

`IMockServerBuild` runs an [Anarchy.Toolkit.Runtime.Web.MockServer](https://www.nuget.org/packages/Anarchy.Toolkit.Runtime.Web.MockServer) host, a runnable project in the solution whose name ends in `MockServer` (or `MockServerProject`) and whose `Program` calls `MockServerEntryPoint.RunAsync(args)`. It is launched by path with `dotnet run`, so the build does not reference it. Parameters: `MockServerPort` (default 6001, or the next free one), `MockServerExpectations`, `MockServerRecord` (default `artifacts/mock-server/requests.jsonl`), `MockServerVerbosity` (`quiet` to `diagnostic`), `MockServerArgs` and `MockServerTimeout`. The boilerplate's `mock-server.mk` exposes each target to Make:

| Target | Make | What it does |
|---|---|---|
| `MockServerRun` | `mock_server` | Runs it in the foreground until Ctrl+C |
| `MockServerStart`, `MockServerStop` | `mock_server_start`, `mock_server_stop` | Starts it in the background (output in `artifacts/mock-server/mock-server.log`), waiting until it is ready, and stops it |
| `TestIntegrationWithMockServer`, `TestEndToEndWithMockServer` | `test_integration_mocked`, `test_end_to_end_mocked` | Runs the tests with `MOCKSERVER_URL` set, starting the server for them (and stopping it after) unless one is already running |
| `MockServerSyncOpenApi` | `mock_server_sync_openapi SPEC=… [OUT=…] [DRY_RUN=true]` | Adds an expectation for each endpoint of an OpenAPI or Swagger specification (file or URL) that has none, into `<tag>.json` files for you to review and commit. It never changes existing expectations: endpoints that changed, disappeared or answer an undocumented status code are only reported |

## Build logs and telemetry

Fallout owns the build's logging: right after creating the build it replaces Serilog's `Log.Logger` with its own
console (themed, honouring `--verbosity`), CI annotation, warnings-summary and `.fallout/temp/build.log` sinks, so a
logger configured in `Main` is discarded. To also send the build logs to your own sinks, add `[BuildTelemetry]` to the
build class, implement `IBuildTelemetry` and return your configuration from `ConfigureBuildLogs`. The attribute wraps
Fallout's logger once it is configured: everything logged through `Log` (targets, toolkit components, tool output)
reaches both, Fallout's sinks are untouched, and your logger is flushed when the build finishes. `IBuildTelemetry`
also exposes `Telemetry`, the toolkit's `AppTelemetry` over the same pipeline (metrics are discarded).

What Fallout keeps to itself:

- events logged while the build is being constructed, before Fallout configures logging;
- the end-of-build "Errors & Warnings" replay and outcome tables, written to a console-only logger;
- the console theme, the `--verbosity` level switch (your sinks choose their own minimum level), the CI annotation
  sink and the log files;
- output written straight to the console rather than logged, such as the toolkit's tables and sections.

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Fallout](https://www.nuget.org/packages/Fallout.Common) (`Fallout.Common`) | The build engine behind the components | Yes: `Target`, `AbsolutePath`, `Solution`, `Project` and the parameter attributes |
| [NuGet.Packaging](https://www.nuget.org/packages/NuGet.Packaging) | Reads and compares package versions (dependency upgrades, NuGet packaging) | No |
| [Docfx](https://www.nuget.org/packages/Docfx.App) (`Docfx.App`) | Generates the API reference | No |
| [Serilog](https://www.nuget.org/packages/Serilog) (through `Infrastructure.Telemetry.Serilog`) | Build logs | No |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
