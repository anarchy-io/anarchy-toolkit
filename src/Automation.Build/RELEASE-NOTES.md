## Releases

### 1.0.0

- Breaking: use descriptive `ConfigurationValueSet`, `EnvironmentValueSet`, and `RuntimeValueSet` types without namespace-clash aliases; public automation default changes are classified as breaking. This newly named package remains at its initial 1.0.0.
- Add `AnarchyBuild` as the standard build host, with Fallout as the underlying engine.
- New package identity, renamed from `Anarchy.Toolkit.Build.Fallout`; versions below are historical releases under that package ID.
- Breaking: replace the ECR-named container targets with registry-neutral `Container*` targets, move the Dockerfile default to `docker/Dockerfile` and the publish output to `artifacts/Container/<app>`. The package remains at its initial 1.0.0 on this branch.
- Add optional unified automation preferences and build preference show/set/unset/configure targets, preserving existing Fallout parameter precedence.
- Added `IBuildTelemetry` and `[BuildTelemetry]`, which add consumer sinks alongside Fallout's logging and expose the
  toolkit telemetry to build code.
- Establish adopter organization conventions and apply application-folder mappings only when folder-based layering is selected.
- `CheckLayering` and `GenerateDependencyMap` resolve optional `layering` overrides from `.anarchy-toolkit.json` against the folder-mode baseline; a missing file or mode is valid.

### Historical releases under `Anarchy.Toolkit.Build.Fallout`

### 3.10.0

- feat(Build.Cli): ship the conventions and onboarding guides in the agent skills

### 3.9.0

- fix(Build.Cli): pack the boilerplate at its source paths
- feat(Build.Cli): ship a generic .gitignore synced through a managed block
- feat(Build.Cli): show, diff and dry-run the boilerplate before installing it
- feat(Build.Cli): keep the packaged .editorconfig in a managed block
- feat(Build.Fallout): make the source and test roots configurable
- feat(Build.Fallout): accept test kind aliases and run unit tests before delivery
- docs(mock-server): add a guide covering recording and migrating from MockServer
- feat(Build.Cli): ship adoption and runtime composition skills

### 3.8.0

- feat(Build.Cli): add opt-in agent skills to the boilerplate
- feat(Build.Fallout): validate and sync agent skills
- fix(Build.Cli): print a plain title on narrow terminals
- fix(Build.Cli): keep shell completion working for including Makefiles

### 3.7.0

- feat(Build.Fallout): run the mock server with or without its UI

### 3.6.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata
- fix(mock-server): default to port 6001, which browsers allow

### 3.6.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1
- feat(Build.Cli): scaffold APIs without the Swagger theme and UIs with the logo and theme toggle

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
