# Anarchy.Toolkit.Build.Cli

The build and command-line operations shared by the toolkit's tooling: the Fallout build components ([`Anarchy.Toolkit.Build.Fallout`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Build.Fallout)) call them from their targets, and the [`anarchy-toolkit`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Build.Cli.Tool) tool from its commands.

```bash
dotnet add package Anarchy.Toolkit.Build.Cli
```

- Solutions: `SolutionBootstrapper` creates a complete solution from a `SolutionPlan` in an empty directory: the Fallout build host (`BuildHostScaffolder`), the packaged Makefile, scripts and settings files (`SolutionScaffolder`), a library per layer and the planned applications. `SolutionWorkspace` reads an existing solution's layout and `ApplicationScaffolder` adds an API, job or UI to it. Existing files are never overwritten.
- Layouts: `SolutionLayout` places libraries and applications by folder (`src/Library/<Layer>`, `src/Api`, `src/Jobs`, `src/Ui`) or by name prefix (`src/<Layer>.<Name>`), and gives the matching `ILayeringBuild` parameters.
- Build operations: layering checks, dependency maps, release versioning and notes, Git hooks, dependency upgrades and NuGet packaging, without depending on Fallout.
- Containers: `LocalContainerTasks` manages a Docker Compose environment; `WaitForRunning` takes an optional readiness callback for containers without a health check.
- Banners: tools built on this package choose their banner with `ConsoleBannerOptions`: Anarchy's logo by default (`AnarchyConsoleBanner`), their own `IConsoleBanner` through `Banner`, or none with `Hide = true`. `Write(console)` prints it, skipping redirected output. The `anarchy-toolkit` tool always prints Anarchy's.

Most consumers should use the `anarchy-toolkit` tool or the Fallout targets rather than this package directly.

## Distributed files

`Boilerplate/` holds the Makefile and scripts installed into generated solutions; together with the repository's `.gitignore`, `.editorconfig`, `global.json` and `nuget.config`, it is embedded as-is. The toolkit repository uses the same files in place through its root `Makefile`, which sets `SCRIPTS_DIR`. The scripts find the project root from `PROJECT_ROOT` (exported by the Makefile), else their parent directory.

## Releases

### 1.3.0

- refactor: `LocalPorts` moved to `Anarchy.Toolkit.Core.Helper` (now references Anarchy.Toolkit.Core)
- feat: `mock-server.mk` boilerplate targets for `IMockServerBuild`

### 1.2.2

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.2.1

- `install-sdk.sh` installs the pinned feature band (e.g. `10.0.1xx`) when `global.json` only lets patches roll forward, the default, instead of a newer feature band its own check rejects; it also announces what it installs before downloading.
- `build.fallout.sh` ignores a `DOTNET_EXE` that is not an executable (e.g. left over from a removed Homebrew SDK) and uses `dotnet` from `PATH`.
- The bootstrap scripts print standard headings and colors through `title.sh`, `subtitle.sh` and `colors.env`; `install-sdk.sh` shows a title when run on its own and a subtitle when part of `install_dev_env`.

### 1.2.0

- Configurable console banners: `IConsoleBanner`, `AnarchyConsoleBanner` and `ConsoleBannerOptions` let tools keep Anarchy's banner, render their own or print none.
- `LocalContainerTasks.WaitForRunning` gains an overload with a readiness callback.

### 1.1.0

- feat(Build.Fallout): replace AWS release flows with Docker and container builds

### 1.0.0

- Introduced solution bootstrapping and scaffolding with configurable layering, application templates and the build operations extracted from Build.Fallout.
