---
name: anarchy
description: "Start here in any .NET solution built on Anarchy Toolkit (a Makefile, build/_build.csproj and Anarchy.Toolkit.* packages), or to create one: the anarchy-toolkit CLI (bootstrap a solution, scaffold APIs, jobs and UIs, install these skills), the solution layout and the rules, plus which anarchy-* skill to load for building, testing, running, releasing or maintaining it."
---

# Anarchy Toolkit

Anarchy Toolkit is a .NET 10 developer platform: layered solutions (Architecture), NuGet packages for persistence,
messaging, observability, hosting and web (Infrastructure), and a Fallout build driven by `make` (Build Ecosystem).
Docs: https://anarchy-io.github.io/anarchy-toolkit/

## When to use

- Creating a solution, or adding an API, job or UI to one: use the CLI below.
- Starting any work in an Anarchy solution: read the rules, then load the skill for the task.
- Adopting or upgrading the toolkit in a repository: load `anarchy-adopt`.

## Rules

- Run `make` from the solution root. `make help` lists every target; `make help COMMAND=<target>` describes one and
  names the skill covering it. Prefer a target to hand-written `dotnet` commands: it encodes the solution's settings.
- Projects only reference their own or a lower layer (by default `Core < Infrastructure < Configuration < Hosting <
  Web`); `make check_layering` enforces it. Put new code in the lowest layer that can hold it.
- Never edit `<Version>` by hand; versions go through `make release_bump` (see `anarchy-release`).
- Generators never overwrite files. On a conflict, report it and ask the user; do not delete to make room.
- Ask the user before publishing, pushing images, destroying data or anything that affects other projects.
- Before finishing a change: `make compile test_unit lint check_layering`.

## Which skill

| Task | Skill |
|---|---|
| Set up, build, run, lint, test, check layering | `anarchy-develop` |
| Local Docker environment, mock server | `anarchy-environment` |
| Modules, settings, health reports, workers, observability | `anarchy-compose` |
| Adopting the toolkit, upgrading it, migrating tests, mocks or DI | `anarchy-adopt` |
| Versions, release notes, packages, container images | `anarchy-release` |
| Dependency upgrades, Git hooks, boilerplate, build targets, these skills | `anarchy-maintenance` |
| Targets the solution added itself | the solution's own skills in `.agents/skills` |

## The anarchy-toolkit CLI

Install or update it with `dotnet tool update --global Anarchy.Toolkit.Build.Cli.Tool`.

| Command | What it does |
|---|---|
| `anarchy-toolkit bootstrap <Name> --output <dir> [--with-skills]` | Creates a layered solution (Fallout build, Makefile, scripts, a library per layer) in an empty or new directory. |
| `anarchy-toolkit scaffold api\|job\|ui <Name>` | Adds a REST API (`Web`), a job such as a Lambda handler (`Hosting`) or a Blazor UI (`Web.Ui`), placed by the solution's layering settings. `--no-swagger-theme` keeps Swagger UI's own look. |
| `anarchy-toolkit skills install [--output <dir>]` | Installs these skills into the solution's `.agents/skills`. |
| `anarchy-toolkit boilerplate list\|show <path>\|diff [<path>...]` | Lists or prints the packaged Makefile, scripts and settings files, or diffs the solution's copies against them, without installing anything. |

- Without arguments, `bootstrap` and `scaffold` start an interactive wizard; only use it in an interactive terminal,
  otherwise ask the user for the names and pass them.
- `bootstrap` lays libraries out by folder (`src/Library/<Layer>`, apps under `src/Api`, `src/Jobs`, `src/Ui`) with the
  default layers; for other layers or name-prefix layout, ask the user to run the wizard.
- After `bootstrap`: `cd <dir> && make install`. After `scaffold`: `make compile check_layering`, then review the new
  application's settings (auth URLs, client IDs) and run `make appsettings` for local ones.
