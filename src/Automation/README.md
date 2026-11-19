# Anarchy.Toolkit.Automation

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Build Ecosystem](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Build · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

The build and command-line operations shared by the toolkit's tooling: Fallout build components ([`Anarchy.Toolkit.Automation.Build`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Automation.Build)) call them from their targets, and the [`anarchy-toolkit`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Automation.Build.Cli) tool from its commands.

```bash
dotnet add package Anarchy.Toolkit.Automation
```

- Solutions: `SolutionBootstrapper` creates a complete solution from a `SolutionPlan` in an empty directory: the Fallout build host (`BuildHostScaffolder`), the packaged Makefile, scripts and settings files (`SolutionScaffolder`), a library per layer and the planned applications. `SolutionWorkspace` reads an existing solution's layout and `ApplicationScaffolder` adds an API, job or UI to it. Existing files are never overwritten.
- Layouts: `SolutionLayout` places libraries and applications by folder (`src/Library/<Layer>`, `src/Api`, `src/Jobs`, `src/Ui`) or by name prefix (`src/<Layer>.<Name>`), and gives the matching `ILayeringBuild` parameters.
- Build operations: layering checks, dependency maps, release versioning and notes, Git hooks, dependency upgrades and NuGet packaging, without depending on Fallout.
- Agent skills: `AgentSkillTasks` reads the Make targets and their `#@` skill markers, creates missing skills from a template and refreshes their generated commands (`Sync`), and checks them (`Validate`); the toolkit's own skills are the boilerplate's opt-in `skills` item.
- Containers: `LocalContainerTasks` manages a Docker Compose environment; `WaitForRunning` takes an optional readiness callback for containers without a health check.
- Banners: tools built on this package choose their banner with `ConsoleBannerOptions`: Anarchy's logo by default (`AnarchyConsoleBanner`), their own `IConsoleBanner` through `Banner`, or none with `Hide = true`. `Write(console)` prints it, skipping redirected output. The `anarchy-toolkit` tool always prints Anarchy's.

Most consumers should use the `anarchy-toolkit` tool or the Fallout targets rather than this package directly.

## Distributed files

`Boilerplate/` holds the Makefile, scripts, `.gitignore`, `.editorconfig` and agent skills (`.agents/skills`, installed only when selected) installed into generated solutions; together with the repository's `global.json` and `nuget.config`, it is embedded as-is. The repository's own `.gitignore` and `.editorconfig` start with the packaged ones and add their own entries below the managed block. `BoilerplateCatalog` lists and reads the packaged files and diffs them against a solution's copies (`UnifiedDiff`), so they can be inspected without installing them. The toolkit repository uses the same files in place through its root `Makefile`, which sets `SCRIPTS_ROOT` so that `SCRIPTS_DIR` (and shell completion) resolve to `src/Automation/Boilerplate/scripts`. The scripts find the project root from `PROJECT_ROOT` (exported by the Makefile), else their parent directory.

## Releases

### 1.0.0

- New package identity, renamed from `Anarchy.Toolkit.Build.Cli`; versions below are historical releases under that package ID.
- Establish adopter organization conventions and ship guidance for selecting folder-based or prefix-based layering.
- Keep application-folder mappings out of prefix-based layering checks.
- Scaffold APIs and jobs with provider-neutral application telemetry.

### Historical releases under `Anarchy.Toolkit.Build.Cli`

### 1.9.0

- feat(Build.Cli): ship the conventions and onboarding guides in the agent skills

### 1.8.0

- fix(Build.Cli): pack the boilerplate at its source paths
- feat(Build.Cli): ship a generic .gitignore synced through a managed block
- feat(Build.Cli): show, diff and dry-run the boilerplate before installing it
- feat(Build.Cli): keep the packaged .editorconfig in a managed block
- docs(mock-server): add a guide covering recording and migrating from MockServer
- feat(Build.Cli): ship adoption and runtime composition skills

### 1.7.0

- feat(Build.Cli): add opt-in agent skills to the boilerplate
- fix(Build.Cli): print a plain title on narrow terminals
- fix(Build.Cli): keep shell completion working for including Makefiles

### 1.6.0

- feat(Build.Fallout): run the mock server with or without its UI

### 1.5.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata
- fix(mock-server): default to port 6001, which browsers allow

### 1.5.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1
- feat(Build.Cli): scaffold APIs without the Swagger theme and UIs with the logo and theme toggle

### 1.4.0

- feat: `OpenApiExpectationSync` adds mock server expectations for OpenAPI/Swagger endpoints (file or URL, JSON or YAML) that have none, never changing existing ones and reporting changed or removed endpoints; `mock_server_sync_openapi` Make target

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
