# Anarchy.Toolkit.Build.Cli

The build and command-line operations shared by the toolkit's tooling: the Fallout build components ([`Anarchy.Toolkit.Build.Fallout`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Build.Fallout)) call them from their targets, and the [`anarchy-toolkit`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/src/Build.Cli.Tool) tool from its commands.

```bash
dotnet add package Anarchy.Toolkit.Build.Cli
```

- Solutions: `SolutionBootstrapper` creates a complete solution from a `SolutionPlan` in an empty directory: the Fallout build host (`BuildHostScaffolder`), the packaged Makefile, scripts and settings files (`SolutionScaffolder`), a library per layer and the planned applications. `SolutionWorkspace` reads an existing solution's layout and `ApplicationScaffolder` adds an API, job or UI to it. Existing files are never overwritten.
- Layouts: `SolutionLayout` places libraries and applications by folder (`src/Library/<Layer>`, `src/Api`, `src/Jobs`, `src/Ui`) or by name prefix (`src/<Layer>.<Name>`), and gives the matching `ILayeringBuild` parameters.
- Build operations: layering checks, dependency maps, release versioning and notes, Git hooks, dependency upgrades and NuGet packaging, without depending on Fallout.

Most consumers should use the `anarchy-toolkit` tool or the Fallout targets rather than this package directly.

## Distributed files

`Boilerplate/` holds the Makefile and scripts installed into generated solutions; together with the repository's `.gitignore`, `.editorconfig`, `global.json` and `nuget.config`, it is embedded as-is. The toolkit repository uses the same files in place through its root `Makefile`, which sets `SCRIPTS_DIR`. The scripts find the project root from `PROJECT_ROOT` (exported by the Makefile), else their parent directory.

## Releases

### 1.1.0

- feat(Build.Fallout): replace AWS release flows with Docker and container builds

### 1.0.0

- Introduced solution bootstrapping and scaffolding with configurable layering, application templates and the build operations extracted from Build.Fallout.
