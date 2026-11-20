# Existing repository

Use this recipe for a .NET repository without `build/_build.csproj`. Keep the service and pipeline working while adding
automation in small, behavior-preserving steps. Confirm the solution file and any existing central package version
management before editing.

## Add the build host

```bash
mkdir -p build .fallout
dotnet new console --framework net10.0 --name _build --output build
dotnet add build/_build.csproj package Anarchy.Toolkit.Automation.Build
printf '{ "Solution": "MyApp.sln" }\n' > .fallout/parameters.json
```

If `.fallout/parameters.json` exists, add `Solution` to it instead; with central package management, add the
`PackageVersion` there. Implement a build class with the components the repository needs, starting with
`IStandardBuild` and `IBoilerplateBuild`; add `ILayeringBuild` once the layout is mapped. `IContainerBuild` needs a
`ContainerRegistry` and is optional.

## Install boilerplate safely

Inspect before writing, then dry-run and install only when there are no unresolved conflicts:

```bash
anarchy-toolkit boilerplate diff Makefile scripts .gitignore .editorconfig
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-items Makefile,scripts,.gitignore,.editorconfig --append-existing --boilerplate-dry-run
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-items Makefile,scripts,.gitignore,.editorconfig --append-existing
```

The target fails without writing when a file would be overwritten. Report conflicts instead of deleting files. Git and
editor settings go inside the `# >>> anarchy-toolkit` managed block; repository overrides stay below it. Show every
warning about tracked files newly ignored. Add `global.json` or `nuget.config` only when absent.

Add the build project to the solution (`dotnet sln add build/_build.csproj`). Run:

```bash
make compile
make test_unit
make check_layering
```

The default layout is folder mode under `src/Library` with standard app folders and layers. If the check reports
violations, explain the options rather than restructuring code: pass explicit folder/prefix arguments or optionally
save layering overrides to `.anarchy-toolkit.json`. Folder mode supports `libraryRoot` and `applicationFolders`; prefix
mode uses project names. Keep a single mode; the tool never infers one.

## Preserve the existing repository's conventions

- Code or tests outside `src` and `tests`: set `SourceRoot` or `TestsRoot` in `.fallout/parameters.json`.
- Test project names outside `Unit`, `Integration`, `EndToEnd`: set `TestKindAliases`, e.g.
  `["EndToEnd=Acceptance"]`. Keep unit tests free of external resources; packing and container publishing run them.
- Pipeline make targets: keep names and meaning. Put custom targets in their own `.mk` file, include it after the
  toolkit Makefile and list it in `MAKE_EXTRA_TARGETS`; annotate them with `#@ <skill>` for skill generation.

Add tests, a mock server, validated settings, one module per adapter, startup/health/workers, then telemetry, following
the [onboarding recipes](guides/onboarding-recipes.md). The [adoption guide](guides/adopting-existing-solution.md)
explains the order and pull-request constraints. Use `anarchy-compose` for modules/settings/workers and
`anarchy-environment` for Docker or the mock server.

To opt out of layering, remove `ILayeringBuild` from `build/Build.cs`; to keep the check with a different layout, adapt
it through preferences. Supported build targets can run on Windows without Bash using
`dotnet run --project build/_build.csproj -- <Target>`; see [Without Make](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html#without-make-including-windows).
