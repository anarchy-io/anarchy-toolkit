# Anarchy I/O Toolkit

![Anarchy I/O logo](docs/assets/images/logo/anarchy-io-rect-800.png)

[![NuGet packages](https://img.shields.io/badge/NuGet-anarchy--io-004880?logo=nuget)](https://www.nuget.org/profiles/anarchy-io)
[![Publish NuGet packages](https://github.com/anarchy-io/anarchy-toolkit/actions/workflows/merge-publish-nuget.yml/badge.svg)](https://github.com/anarchy-io/anarchy-toolkit/actions/workflows/merge-publish-nuget.yml)
[![Publish documentation](https://github.com/anarchy-io/anarchy-toolkit/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/anarchy-io/anarchy-toolkit/actions/workflows/publish-docs.yml)

**A .NET developer platform.** An integrated ecosystem for building maintainable .NET applications through consistent architecture, reusable infrastructure and automated development workflows. *Build without limits.*

The toolkit is built around three pillars. Each one can be adopted on its own, and they are designed to work consistently together:

| Pillar | What it provides |
|---|---|
| **[Architecture](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html)** | Application structure, dependency boundaries (`Core < Infrastructure < Configuration < Hosting < Web < Automation` in this repository), DI-module composition and engineering conventions. |
| **[Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/packages/)** | Independently published NuGet packages for persistence, messaging, telemetry, configuration, hosting, APIs, UIs and testing. |
| **[Build Ecosystem](https://anarchy-io.github.io/anarchy-toolkit/getting-started/)** | Anarchy automation components and the `anarchy-toolkit` CLI: scaffolding, validation, tests, packaging, versioning and releases, powered by [Fallout Build](https://fallout.build/). |

> 📖 **Using the toolkit?** Start with the documentation at **<https://anarchy-io.github.io/anarchy-toolkit/>** ([overview](https://anarchy-io.github.io/anarchy-toolkit/overview/), [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/), [guides](https://anarchy-io.github.io/anarchy-toolkit/guides/)). Packages are published on [NuGet.org](https://www.nuget.org/profiles/anarchy-io).

This README is for working **on** the toolkit itself: setting up a local environment, building, testing and releasing. Read [CONTRIBUTING.md](.github/CONTRIBUTING.md) for the contribution workflow, commit conventions and maintainer notes.

## Prerequisites

- The .NET 10 SDK (see `global.json` for the minimum version and roll-forward policy). `make install_sdk` can install it for you.
- Git, Bash and Make.
- Ruby and Bundler, only to preview the documentation site.

Dependencies restore from nuget.org only; `nuget.config` declares the source and its mapping.

## Setting up

```bash
git clone git@github.com:anarchy-io/anarchy-toolkit.git
cd anarchy-toolkit
make install                    # configure the local .NET environment, restore and compile
make install WITH_HOOKS=true    # ...and also install the Git hook
```

`make install` persists development settings in your shell environment file and trusts the local HTTPS certificate; use `ENV=ci` to skip this setup. Narrower targets cover parts of it:

```bash
make install_dev_env  # install/validate the SDK, set ASPNETCORE_ENVIRONMENT and trust development certificates
make install_sdk      # install or validate the SDK required by global.json
make appsettings      # generate untracked appsettings.Development.json from each appsettings.json.template (RESET=true asks for every key again)
```

Run `make help` for the full, self-documenting list of targets.

## Everyday targets

| Target | What it does |
|---|---|
| `make compile` | Builds the solution (`Compile`, warnings as errors). |
| `make build` | Cleans, then builds. |
| `make lint` | Runs `dotnet format --verify-no-changes`. |
| `make lint_fix` | Runs `dotnet format`; `AUTO_STAGE=true` stages eligible fixes again. |
| `make test` | Runs every test project (unit, integration and end-to-end). |
| `make test_unit` | Runs unit tests only; `COMPILE=true` rebuilds first. |
| `make check_layering` | Verifies that dependencies only point down the layers. |
| `make dependency_map` | Regenerates [`docs/dependency-map.html`](docs/dependency-map.html); commit it when project references change. |
| `make config_show` / `make config_set KEY=… VALUE=…` | Inspect or explicitly edit optional `.anarchy-toolkit.json` automation overrides; `config_unset` clears one and `config_configure` opens the wizard. |
| `make list_projects` | Lists every project in the solution. |
| `make list_packages` | Lists every package ID and version. |
| `make run_dotnet_tool TOOL_ARGS="…"` | Runs the `anarchy-toolkit` tool from source. |

Auto-staging only considers files that were already staged, with no unstaged changes, before linting; partially staged files are left for manual review. Avoid editing files while linting runs.

Tests run on **Microsoft.Testing.Platform** (`global.json` → `test.runner`). `tests/Unit/Unit.csproj` holds every unit test, in folders mirroring the `src/` projects (`src/Core.Events` → `tests/Unit/Core/Events/`). The Anarchy test targets route xUnit v3 projects through MSBuild's `Test` target and any VSTest project through `dotnet test`.

Plain `dotnet` also works:

```bash
dotnet build Anarchy.Toolkit.sln -c Release
dotnet pack  Anarchy.Toolkit.sln -c Release -o ./artifacts/packages
```

### Running Anarchy targets directly

```bash
make anarchy TARGET=Pack                  # e.g. Compile, Lint, TestAll, Pack, Publish, ListProjects
make anarchy TARGET="Lint --IsCiBuild true"
make anarchy_help                         # list all Anarchy targets and parameters
```

`make fallout`, `make nuke`, `make fallout_help` and `make nuke_help` remain as aliases for compatibility.

### Git hooks

```bash
make git_hooks_enable OPTIONS="--dry-run --skip-test-runtime"  # preview
make git_hooks_show                                            # inspect
make git_hooks_disable                                         # remove
```

Replacing an existing hook requires `--force`.

## Releasing

| Target | What it does |
|---|---|
| `make release_bump RELEASE_PROJECT=Core.Events RELEASE_BUMP=patch` | Bumps a package and all its dependents. |
| `make release_notes RELEASE_BASE=origin/main RELEASE_HEAD=HEAD` | Fills missing release notes in the affected packages' READMEs. |
| `make pack` | Packs every package into a fresh `artifacts/packages/<run-id>/` directory. |
| `make publish SOURCE=… [API_KEY=…]` | Packs and pushes to a NuGet feed. |

Every project carries its own `<Version>`. A `ProjectReference` between toolkit projects becomes a NuGet `<dependency>` when packed, so the in-repository development loop and the published dependency metadata both work without switching to `PackageReference`s. Pushes to `main` detect changes, bump the affected packages and publish them; see [CONTRIBUTING.md](.github/CONTRIBUTING.md#publishing-packages-maintainers) and the [Anarchy Automation Builds guide](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html#automatic-releases).

### Upgrading dependencies

```bash
make upgrade_dependencies  # bump centrally managed package versions (dotnet-outdated)
```

Upgrades ask for confirmation. `LOCK=Major` (the default), `Minor` or `None` controls the version lock; in non-interactive use, `INSTALL_TOOL=true` and `UPGRADE=true` explicitly permit installing the tool and applying upgrades. The upgrade also updates literal, unconditional versions in the root `Directory.Packages.props` for outdated transitive dependencies, then restores the solution to catch incompatible pins. Explicit version ranges, conditional versions and nested central files may need manual updates if the restore fails. Review all changes before committing.

## Repository layout

| Path | Contents |
|---|---|
| `src/` | One project per package, named `Anarchy.Toolkit.<project name>`. Each has its own `README.md`, shipped to NuGet.org and to the documentation site. |
| `tests/` | One test project per test kind. |
| `samples/` | [Sample applications](samples/README.md) built on the toolkit. Never packed. |
| `build/` | The Anarchy build project (see below). |
| `docs/` | The [documentation site](#working-on-the-documentation). |
| `scripts/make/targets/monorepo.mk` | Repository-only Make targets (the tool runner and the documentation targets). |
| `.fallout/` | Fallout's folder: marks the repository root, pins `Solution` and `Configuration` in `parameters.json`, and holds the generated `build.schema.json`. The engine fixes its name. |
| `.anarchy-toolkit.json` | Optional solution-root overrides for automation and layering preferences. No file is required; the toolkit defaults remain usable without it. See [settings and precedence](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html). |

The distributed Makefile and scripts live in `src/Automation/Boilerplate/`, the `Automation` package's source of truth. The root `Makefile` includes them in place, and solutions generated by the tool get them at their root:

- `Makefile` + `scripts/make/`: self-documenting Make targets and help (parsed from `#?`/`#+` markers).
- `scripts/bootstrap/`: SDK and development-environment installation and shell-profile helpers.
- `scripts/console/` and `scripts/shared/`: display utilities and shared colors.
- `scripts/build.anarchy.sh`: launcher that reloads the shell environment and runs the build project. Set `ANARCHY_RELOAD_ENV=false` to skip reloading, or set `ANARCHY_LOG_LEVEL` to change the engine log level.

### The Anarchy build project

`build/` is a thin consumer of the solution's own build components, `src/Automation.Build`. It references them as a project, so the build always runs the code it builds and packs:

- `Build.cs`: `partial class Build : AnarchyBuild, INuGetBuild, …`. The standard targets come from the library.
- `Build.CodeGeneration.cs`: `GenerateTools`, which regenerates the library's CLI tool wrappers from their specifications.
- `Directory.Build.targets`: imports the library's `buildTransitive` targets, which load the NuGet.* assemblies from the installed SDK so the build keeps working on newer SDK feature bands.

## Working on the documentation

The site under `docs/` is built with [Jekyll](https://jekyllrb.com/) and the [Minimal Mistakes](https://mmistakes.github.io/minimal-mistakes/) theme, and published to GitHub Pages by the [`Publish documentation`](.github/workflows/publish-docs.yml) workflow on every push to `main` (set **Settings → Pages → Source** to **GitHub Actions**).

Pages are plain Markdown, readable on GitHub too: titles come from each page's first heading, `.md` links are rewritten to the generated pages, and ` ```mermaid ` blocks render as diagrams. The package and samples pages are not committed under `docs/`: `docs/scripts/sync-readmes.sh` copies them from `src/*/README.md` and `samples/README.md` before every build, so each package README stays the single source of truth.

```bash
make docs_serve   # sync the READMEs, then serve with live reload at http://localhost:4000/anarchy-toolkit/
make docs_build   # sync the READMEs, then build into docs/_site
```

Sidebars are defined in `docs/_data/navigation.yml`, and brand colors and fonts in `docs/assets/css/main.scss`.

## License

Copyright 2026 Leandro Melão Medeiros. Licensed under the [Apache License 2.0](LICENSE). It covers the toolkit's original code; third-party components keep their own licenses. See [NOTICE](NOTICE) for attribution.
