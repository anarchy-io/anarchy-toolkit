# Anarchy.Toolkit.Automation

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Build Ecosystem](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Automation · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

The build and command-line operations shared by the toolkit's tooling: Anarchy build components ([`Anarchy.Toolkit.Automation.Build`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Automation.Build)) call them from their targets, and the [`anarchy-toolkit`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Automation.Build.Cli) tool from its commands. The build is powered by [Fallout Build](https://fallout.build/).

```bash
dotnet add package Anarchy.Toolkit.Automation
```

- Solutions: `SolutionBootstrapper` creates a complete solution from a `SolutionPlan` in an empty directory: the Anarchy build host (`BuildHostScaffolder`), the packaged Makefile, scripts and settings files (`SolutionScaffolder`), a library per layer and the planned applications. `SolutionWorkspace` reads an existing solution's layout and `ApplicationScaffolder` adds an API, job or UI to it. Existing files are never overwritten.
- Layouts and preferences: `SolutionLayout` places libraries and applications by folder (`src/Library/<Layer>`, `src/Api`, `src/Jobs`, `src/Ui`) or by name prefix (`src/<Layer>.<Name>`). `LayeringConfiguration` resolves explicit layout arguments, optional `layering` values in the solution-root `.anarchy-toolkit.json`, then the folder-mode baseline; it validates the effective layout without requiring saved settings or inferring the mode. `AutomationPreferences` reads and edits typed optional overrides shared by CLI and Fallout, keeping the CLI independent of Fallout. Explicit edits validate the merged document and write atomically; normal operations do not save preferences.
- Build operations: layering checks, dependency maps, release versioning and notes, Git hooks, dependency upgrades and NuGet packaging, without depending on Fallout.
- Preference editing: `ShowAnarchyPreferences`, `SetAnarchyPreference`, `UnsetAnarchyPreference` and `ConfigureAnarchyPreferences` share the reader/writer used by both entry points; see [Automation Preferences](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html).
- Agent skills: `AgentSkillTasks` reads the Make targets and their `#@` skill markers, creates missing skills from a template and refreshes their generated commands (`Sync`), and checks them (`Validate`); the toolkit's own skills are the boilerplate's opt-in `skills` item.
- Containers: `LocalContainerTasks` manages a Docker Compose environment; `WaitForRunning` takes an optional readiness callback for containers without a health check.
- Banners: tools built on this package choose their banner with `ConsoleBannerOptions`: Anarchy's logo by default (`AnarchyConsoleBanner`), their own `IConsoleBanner` through `Banner`, or none with `Hide = true`. `Write(console)` prints it, skipping redirected output. The `anarchy-toolkit` tool always prints Anarchy's.

Most consumers should use the `anarchy-toolkit` tool or the Anarchy build targets rather than this package directly.

## Distributed files

`Boilerplate/` holds the Makefile, scripts, `.gitignore`, `.editorconfig` and agent skills (`.agents/skills`, installed only when selected) installed into generated solutions; together with the repository's `global.json` and `nuget.config`, it is embedded as-is. The repository's own `.gitignore` and `.editorconfig` start with the packaged ones and add their own entries below the managed block. `BoilerplateCatalog` lists and reads the packaged files and diffs them against a solution's copies (`UnifiedDiff`), so they can be inspected without installing them. The toolkit repository uses the same files in place through its root `Makefile`, which sets `SCRIPTS_ROOT` so that `SCRIPTS_DIR` (and shell completion) resolve to `src/Automation/Boilerplate/scripts`. The scripts find the project root from `PROJECT_ROOT` (exported by the Makefile), else their parent directory.

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Microsoft.OpenApi](https://www.nuget.org/packages/Microsoft.OpenApi) (with `Microsoft.OpenApi.YamlReader`) | Reads OpenAPI specifications to sync mock-server expectations | No |
| [Serilog](https://www.nuget.org/packages/Serilog) | Logs the operations (Git hooks, lint staging) | No |
| [Spectre.Console](https://www.nuget.org/packages/Spectre.Console) | The banner and console progress | Yes: `ConsoleBannerOptions.Write` takes an `IAnsiConsole` |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
