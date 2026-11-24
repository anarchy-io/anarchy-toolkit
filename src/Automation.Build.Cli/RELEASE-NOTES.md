## Releases

### 3.0.0

- Breaking: scaffold jobs as runnable workers by default; `--job-mode once` selects a one-shot job.
- Ask for optional container and Lambda entry points in application wizards, with corresponding non-interactive flags.

### 2.1.0

- Update embedded Web package versions and dependent package references.

### 2.0.1

- Updated the Automation dependencies for their bundled telemetry guidance.

### 2.0.0

- Breaking: Generate release entries in each package's `RELEASE-NOTES.md` instead of its README.
- fix(Automation.Build): Handle Windows directory containment
- fix(Web): Support Windows paths and host architecture
- chore(build): Format package release notes property

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
