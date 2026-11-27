---
name: anarchy-develop
description: "Day-to-day development in an Anarchy Toolkit solution: set up the environment and SDK, restore, compile, run applications, generate local appsettings, lint, run unit, integration and end-to-end tests, check the layered architecture, refresh the dependency map and run the pull request checks. Use it for any build, run or test task."
---

# Develop an Anarchy solution

## When to use

- Setting up a freshly cloned or bootstrapped solution.
- Checking a change before committing it, or reproducing the pull request checks.
- Running one or more applications locally.

## Workflows

**First setup**: `make install` (SDK, restore, compile; `ENV=ci` skips the SDK step), then `make appsettings` if the
applications have `appsettings.json.template` files. It prompts for each value: run it in an interactive terminal or
ask the user for the values. Never commit `appsettings.Development.json`.

**Chaining targets (keep runs short)**: every make target that wraps a build target first builds `build/_build.csproj`,
then runs that target with its dependencies, usually `Restore` and often `Compile`. `make test_unit lint check_layering`
therefore repeats all of that three times, and so does the output you read. To run several checks:
- Prefer an existing combined target: `make pull_request` (lint, layering, all tests) or `make test` (all tests).
- Otherwise run the build targets in one call, so shared dependencies run once:
  `dotnet run --project build/_build.csproj -- Lint CheckLayering TestUnit`. Target names are the PascalCase ones in
  `make anarchy_help` (`make check_layering` is `CheckLayering`, `make dependency_map` is `GenerateDependencyMap`).
- Already compiled and nothing changed since? Add `--skip Restore Compile` to skip those dependencies (`--skip` with no
  names skips every dependency). Do not skip after editing code.
- Use single make targets for one-off runs, and run a test project directly for a subset (see **Tests**).

**Before committing**: `dotnet run --project build/_build.csproj -- Lint CheckLayering TestUnit` (one restore and compile
for all three), or `make pull_request` when the other test kinds can run too.
- `lint` fails on formatting; `make lint_fix` rewrites the files, then review the diff.
- `check_layering` fails when a project, file import or folder points to a higher layer. Move the code down a layer or
  invert the dependency; never reference a higher layer. If the layering itself looks wrong, ask the user.
- **Layering configuration**: before adding a project, scaffolding or judging architecture, read any `layering`
  overrides in `.anarchy-toolkit.json`. `mode` is `folder` (the layer is the project's application folder, else its
  first folder under `libraryRoot`) or `prefix` (the name up to the first dot); with no override, folder mode uses
  `src/Library`, the `src/Apps/{Apis,Jobs,Ui}` application folders and `Core < Infrastructure < Bootstrap < Runtime < Apps`.
  Place projects so the effective mode finds their layer and never mix modes. Missing settings are valid; pass
  operation arguments or explicitly save preferences instead of inferring the layout. Use `anarchy-toolkit config`
  or the `config_show`, `config_set`, `config_unset`, `config_configure` Make wrappers for deliberate preference
  editing. Fallout CLI/environment/parameter resolution takes precedence over saved overrides, then code defaults.
- Analyzer warnings are errors in Release builds; fix them instead of suppressing them.
- When projects, references or packages changed, add `GenerateDependencyMap` to the same call and commit
  `docs/dependency-map.html`.

**Tests**: unit tests need nothing else. If the build opts into `ITestEnvironmentBuild`, integration and end-to-end
targets start the Compose environment and wait for configured readiness; otherwise start required local services with
`make start` (see `anarchy-environment`). `make test_integration_mocked` also manages the mock server. Tests that assert
telemetry use `FixtureTelemetry` from `TestCaseBase` (an in-memory `AppTelemetry`). For a subset, run the test project
directly, e.g. `dotnet run --project tests/Unit -c Release -- -class "*OrderTest"`. `COMPILE=true` rebuilds first.

**Test kinds**: the build finds tests by kind (`Unit`, `Integration`, `EndToEnd`) from project names such as
`tests/Unit/Unit.csproj` or `Shop.Tests.Unit`. Keep one project per kind: projects do not run in parallel with each
other, while tests inside one do. Unit tests must not need external resources: packing and container publishing run
them, and never the other kinds. A repository with other names (e.g. `*.Tests.Acceptance`) maps them with
`"TestKindAliases": ["EndToEnd=Acceptance"]` in `.fallout/parameters.json` instead of renaming; another scheme means
overriding `IsTestProjectOfKind` in `build/Build.cs` (ask the user first).

**Run applications**: `make list_projects` shows the names; `make run_app APP_NAME=<name>` or
`make run_apps APP_NAMES=Api,Ui`. They run in the foreground until stopped: start them in the background or a separate
terminal if you need to keep working, and stop them when done.

**Pull request**: `make pull_request` (add `ENV=ci` to match CI exactly) runs lint, the layering check and all tests.

**Windows without Make**: supported build targets can run directly with
`./scripts/build.anarchy.ps1 <Target> [--parameter value]` (PowerShell launcher, same defaults as
`build.anarchy.sh`) or `dotnet run --project build/_build.csproj -- <Target> [--parameter value]`. `Compile`, test targets, `Lint`,
`CheckLayering` and `Pack` do not require Bash. Make workflows, SDK bootstrap scripts and generated Git hooks still
need Make and Bash; see [Without Make](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html#without-make-including-windows).

**Something odd**: `make info` prints the environment; `make clean` or `make rebuild` start over.

## Commands

<!-- anarchy:commands:begin (generated by 'make skills_sync' from the Makefiles' #? comments) -->
| Command | What it does | Parameters |
|---|---|---|
| `make clean` | Cleans build outputs across the solution |  |
| `make build` | Cleans and builds the full solution |  |
| `make compile` | Compiles the solution |  |
| `make list_projects` | Lists all projects within the solution |  |
| `make run_app` | Runs one of the runnable (OutputType=Exe) projects under src/ | `APP_NAME` (str): Name of the application to run |
| `make run_apps` | Runs multiple runnable (OutputType=Exe) projects under src/ in parallel | `APP_NAMES` (str): Comma-separated names of the applications to run |
| `make lint` | Runs linter analysis on the whole solution |  |
| `make lint_fix` | Runs the linter auto-fixer on the whole solution | `AUTO_STAGE` (bool): Restages files during pre-commit |
| `make pull_request` | Runs all tests and validations for the Pull Request CI-pipeline |  |
| `make test` | Runs all solution tests |  |
| `make test_unit` | Runs unit tests | `COMPILE` (bool): Rebuilds the solution before running tests (DEFAULT=false) |
| `make test_integration` | Runs integration tests, preparing local services if the build opts in | `COMPILE` (bool): Rebuilds the solution before running tests (DEFAULT=false) |
| `make test_end_to_end` | Runs end-to-end tests | `COMPILE` (bool): Rebuilds the solution before running tests (DEFAULT=false) |
| `make check_layering` | Verifies the layered architecture: references, imports, layer folders |  |
| `make dependency_map` | Generates docs/dependency-map.html: project references, layers and where the code lives |  |
| `make info` | Prints out host-relevant information |  |
| `make install` | Sets up the development environment, restores dependencies and compiles | `ENV` (str): Defaults to "dev"; "ci" skips local environment setup<br>`WITH_HOOKS` (bool): Enable local GIT hooks (DEFAULT=false) |
| `make install_dev_env` | Installs the SDK and configures the local development environment |  |
| `make install_sdk` | Installs or validates the .NET SDK required by global.json |  |
| `make install_dependencies` or `make restore` | Installs the dependency packages |  |
| `make rebuild` | Cleans and reinstalls the local development environment |  |
| `make appsettings` | Generates the untracked appsettings.Development.json files from their appsettings.json.template | `RESET` (bool): Ask again for every template key, offering the current value as default (DEFAULT=false) |
<!-- anarchy:commands:end -->
