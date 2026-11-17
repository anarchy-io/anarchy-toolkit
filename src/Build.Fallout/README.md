# Anarchy.Toolkit.Build.Fallout

Reusable [Fallout](https://fallout.build/) build components for .NET repositories.

```bash
dotnet add build/_build.csproj package Anarchy.Toolkit.Build.Fallout
```

Implement the components appropriate to your repository on a `FalloutBuild` class. For example, a package-only build can implement `INuGetBuild`, `ILayeringBuild`, `IDependencyMapBuild`, `IDependencyUpgradeBuild` and `IGitHooksBuild`, with an empty `Artifacts` collection. Use `IBoilerplateBuild` for scaffolding, add `IApiReferenceBuild` for a DocFX API reference site, or add the container (`IContainerBuild`, `IDockerBuild`) components as needed. External deployment and publication targets require their respective tools and credentials.

```csharp
using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;

class Build : FalloutBuild, INuGetBuild, ILayeringBuild, IDependencyMapBuild,
              IDependencyUpgradeBuild, IGitHooksBuild
{
    public IReadOnlyCollection<Artifact> Artifacts => [];
    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
}
```

Create `.fallout/parameters.json` with the solution path, then run `dotnet run --project build/_build.csproj -- --help` to see targets and parameters. The included scaffolding target writes its solution setting to `.fallout/parameters.json`; it will not overwrite existing files.

See the [Fallout builds guide](https://anarchy-io.github.io/anarchy-toolkit/guides/fallout-builds.html) for component details and package publishing.

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

For containers without a health check, override `DockerReadinessCheck` in the build to add your own; it is called once a second with the container's name until it returns true:

```csharp
Func<string, bool>? IDockerBuild.DockerReadinessCheck => container =>
{
    try { using var client = new TcpClient("localhost", 5432); return true; }
    catch (SocketException) { return false; }
};
```

## Starting from an empty folder

For a guided setup, including APIs, jobs and UIs, install the `Anarchy.Toolkit.Build.Cli.Tool` global tool and run `anarchy-toolkit bootstrap`. See the [tool README](../Build.Cli.Tool/README.md). To set up the build host manually instead:

With the .NET 10 SDK, Bash and Make installed, create a build host first:

```bash
mkdir -p build .fallout
dotnet new console --framework net10.0 --name _build --output build
dotnet add build/_build.csproj package Anarchy.Toolkit.Build.Fallout
printf '{}\n' > .fallout/parameters.json
```

Use the example above in `build/Program.cs`, adding `IBoilerplateBuild` to the class. Then generate a starter solution and try the build:

```bash
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-solution-name MyApp
make compile
make check_layering
```

By default, scaffolding creates a solution containing the build and a starter `src/Core` project, along with `tests/`, `samples/`, `docs/`, `Makefile`, scripts and configuration files. Review the supplied .NET SDK and test-runner settings and enable or adapt the optional Make targets for your build components and tools.

### Choosing what to install

Use `--boilerplate-items` for a comma-separated allowlist and `--boilerplate-exclude` to remove items. Names are case-insensitive; omit the allowlist to select everything.

| Item | Effect |
|---|---|
| `solution` | Create the named `.sln`, add the existing build and selected Core project, and set `Solution` in `.fallout/parameters.json`. Requires `--boilerplate-solution-name`. |
| `core` | Create `src/Core/Core.csproj`. Add it to a solution yourself if `solution` is excluded. |
| `tests`, `samples`, `docs` | Create the directory and its `.gitkeep`. |
| `Makefile`, `.gitignore`, `.editorconfig`, `global.json`, `nuget.config` | Install the corresponding packaged file. |
| `Directory.Build.props` | Create the SQL resource embedding rule if the file is absent. |
| `scripts` | Install all scripts; individual paths such as `scripts/build.fallout.sh` or `scripts/bootstrap` can also be selected or excluded. |

`all` selects every item and `none` selects nothing. Unknown names fail before any files are written. When selecting individual scripts, include their helpers: the launcher needs `scripts/shared/colors.env`, `scripts/console/subtitle.sh` and, unless environment reloading is disabled, `scripts/bootstrap/shell-profile-path.sh`.

Existing `.gitignore` and `.editorconfig` files are untouched unless `--append-existing` (or `--apend-existing`) is supplied; packaged content is then appended at most once. Review the result because later rules can change earlier ones. The same option adds the packaged `test` section (`"runner": "Microsoft.Testing.Platform"`, which `dotnet test` needs to run xUnit.net v3 projects) to an existing `global.json` that does not have one; the rest of the file is kept, and a file with comments is left alone. Existing `nuget.config` and `Directory.Build.props` are always preserved, as is `global.json` otherwise. The target refuses to overwrite a solution, selected script, Makefile, starter project, `.gitkeep` or existing `Solution` parameter. If an operation fails partway through, inspect the output before retrying.

## Mock server

`IMockServerBuild` runs an [Anarchy.Toolkit.Web.MockServer](https://www.nuget.org/packages/Anarchy.Toolkit.Web.MockServer) host, a runnable project in the solution whose name ends in `MockServer` (or `MockServerProject`) and whose `Program` calls `MockServerEntryPoint.RunAsync(args)`. It is launched by path with `dotnet run`, so the build does not reference it. Parameters: `MockServerPort` (default 6000, or the next free one), `MockServerExpectations`, `MockServerRecord` (default `artifacts/mock-server/requests.jsonl`), `MockServerVerbosity` (`quiet` to `diagnostic`), `MockServerArgs` and `MockServerTimeout`. The boilerplate's `mock-server.mk` exposes each target to Make:

| Target | Make | What it does |
|---|---|---|
| `MockServerRun` | `mock_server` | Runs it in the foreground until Ctrl+C |
| `MockServerStart`, `MockServerStop` | `mock_server_start`, `mock_server_stop` | Starts it in the background (output in `artifacts/mock-server/mock-server.log`), waiting until it is ready, and stops it |
| `TestIntegrationWithMockServer`, `TestEndToEndWithMockServer` | `test_integration_mocked`, `test_end_to_end_mocked` | Runs the tests with `MOCKSERVER_URL` set, starting the server for them (and stopping it after) unless one is already running |
| `MockServerSyncOpenApi` | `mock_server_sync_openapi SPEC=… [OUT=…] [DRY_RUN=true]` | Adds an expectation for each endpoint of an OpenAPI or Swagger specification (file or URL) that has none, into `<tag>.json` files for you to review and commit. It never changes existing expectations: endpoints that changed, disappeared or answer an undocumented status code are only reported |

## Releases

### 3.5.0

- feat: `MockServerSyncOpenApi` target (`MockServerOpenApi`, `MockServerOpenApiOutput`, `MockServerOpenApiDryRun`) adding expectations for new OpenAPI endpoints

### 3.4.0

- feat: `IMockServerBuild`, running a mock server host in the foreground, in the background or around integration and end-to-end tests

### 3.3.1

- docs: expand XML docblocks across all projects for the DocFX API reference

### 3.3.0

- feat(Build.Fallout): add IApiReferenceBuild generating a DocFX API reference from the compiled assemblies

### 3.2.0

- Target output is formatted with Spectre.Console: `ListProjects`, `ListPackages` and the `RunApp`/`RunApps` app hints print tables, `GenerateAppSettings` prompts per file with masked input for secrets, the release targets end with a summary of version changes (changed or dependent), and `Publish` numbers each push and prints a summary.
- The dependency upgrade lists outdated packages within its version lock (`--highest-minor` for `Major`, `--highest-patch` for `Minor`), and prints each step under its own heading.
- `System.Security.Cryptography.Xml` aligned with the .NET 10 LTS (10.0.15, down from 11.0.0).

### 3.1.0

- `IDockerBuild` gains the overridable `DockerReadinessCheck`, a custom check `DockerWaitRunning` polls once the container is running, for containers without a health check.

### 3.0.0

- feat(Build.Fallout): replace AWS release flows with Docker and container builds

### 2.0.0

- **Breaking:** layering, dependency map, release version and notes, Git hook, lint staging and local process tasks, their entities and the connection-string helpers moved to `Anarchy.Toolkit.Build.Cli` (namespaces `Anarchy.Toolkit.Build.Cli.Core.*` and `.Infrastructure.Helper`). Targets are unchanged; update `using` directives that referenced them directly.
- `ILayeringBuild` gains the `LayeringLayers` and `LayeringApplicationFolders` parameters, so layers and application folders (e.g. `src/Api=Web`) can be configured in `.fallout/parameters.json`.
- The packaged Makefile and scripts now come from `Anarchy.Toolkit.Build.Cli`.

### 1.0.2

- chore(deps): update AWS SDK packages
