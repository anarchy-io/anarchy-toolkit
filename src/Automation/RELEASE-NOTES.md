## Releases

### 2.0.1

- Updated the bundled telemetry decision to describe the optional DogStatsD adapter.

### 2.0.0

- Breaking: Generate release entries in each package's `RELEASE-NOTES.md` instead of its README.
- chore(build): Format package release notes property

### 1.0.0

- New package identity, renamed from `Anarchy.Toolkit.Build.Cli`; versions below are historical releases under that package ID.
- Breaking: rename the public build-package version constant to `ToolkitPackageVersions.AutomationBuild`, the engine-file helper to `BuildEngineParameters`, and the `BuildHostScaffolder.Create` version parameter to `automationBuildVersion`; public automation default changes are also classified as breaking. This newly named package remains at its initial 1.0.0.
- Expose `make anarchy` and `make anarchy_help` as the canonical build commands, with `fallout` and `nuke` compatibility aliases, and rename the launcher to `scripts/build.anarchy.sh`.
- Add optional root-level automation overrides in `.anarchy-toolkit.json`, with shared validated read/edit operations and no required configuration file.
- Establish adopter organization conventions and ship guidance for selecting folder-based or prefix-based layering.
- Keep application-folder mappings out of prefix-based layering checks.
- Resolve optional layering overrides against the in-code folder baseline; a missing file or mode is valid and does not trigger layout inference. The unified schema covers all persistable automation preferences.
- Scaffold APIs and jobs with provider-neutral application telemetry.
- Add named `monorepo` and `single-app` layout presets, including one custom application-folder mapping, without changing the in-code monorepo baseline.
- Show the source of each effective saved/default preference and accept invocation-supplied values from the Fallout build target.

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
- `build.anarchy.sh` ignores a `DOTNET_EXE` that is not an executable (e.g. left over from a removed Homebrew SDK) and uses `dotnet` from `PATH`.
- The bootstrap scripts print standard headings and colors through `title.sh`, `subtitle.sh` and `colors.env`; `install-sdk.sh` shows a title when run on its own and a subtitle when part of `install_dev_env`.

### 1.2.0

- Configurable console banners: `IConsoleBanner`, `AnarchyConsoleBanner` and `ConsoleBannerOptions` let tools keep Anarchy's banner, render their own or print none.
- `LocalContainerTasks.WaitForRunning` gains an overload with a readiness callback.

### 1.1.0

- feat(Build.Fallout): replace AWS release flows with Docker and container builds

### 1.0.0

- Introduced solution bootstrapping and scaffolding with configurable layering, application templates and the build operations extracted from Build.Fallout.
