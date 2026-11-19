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
- Identify the solution's selected project-layering mode before scaffolding or checking: folder-based is recommended
  for adopters; project-name prefixes are also supported and used by the toolkit. Use one mode throughout the solution;
  do not mix detection modes or bypass checks to do so. Inspect the configured roots and application mappings.
- In a monorepo, prefer shared library dependencies and DI modules or wrappers, but let the developer weigh reuse against
  unused transitive assemblies and larger artifacts. Shared Web is optional; single applications can place their layer
  projects directly under `src/`.
- Never edit `<Version>` by hand; versions go through `make release_bump` (see `anarchy-release`).
- Generators never overwrite files. On a conflict, report it and ask the user; do not delete to make room.
- Ask the user before publishing, pushing images, destroying data or anything that affects other projects.
- Before writing or reviewing code, load `anarchy-conventions` for the toolkit's coding conventions. They are
  recommendations: the solution's own `AGENTS.md`, ADRs and established code win where they differ.
- Before finishing a change: `make compile test_unit lint check_layering`.

## Which skill

| Task | Skill |
|---|---|
| Where code goes, naming, async, exceptions, enums, endpoints, logging, tests: the conventions and why | `anarchy-conventions` |
| Set up, build, run, lint, test, check layering | `anarchy-develop` |
| Local Docker environment, mock server | `anarchy-environment` |
| Modules, settings, health reports, workers, observability | `anarchy-compose` |
| Adopting the toolkit, upgrading it, migrating tests, mocks or DI; adoption pitfalls | `anarchy-adopt` |
| Versions, release notes, packages, container images | `anarchy-release` |
| Dependency upgrades, Git hooks, boilerplate, build targets, these skills | `anarchy-maintenance` |
| Targets the solution added itself | the solution's own skills in `.agents/skills` |

## The anarchy-toolkit CLI

Install or update it with `dotnet tool update --global Anarchy.Toolkit.Build.Cli.Tool`.

| Command | What it does |
|---|---|
| `anarchy-toolkit bootstrap <Name> --output <dir> [--with-skills] [--layering folder\|prefix]` | Creates a layered solution (Fallout build, Makefile, scripts, a library per layer) in an empty or new directory. |
| `anarchy-toolkit scaffold api\|job\|ui <Name>` | Adds a REST API (`Web`), a job such as a Lambda handler (`Hosting`) or a Blazor UI (`Web.Ui`), placed by the solution's layering settings. `--no-swagger-theme` keeps Swagger UI's own look. |
| `anarchy-toolkit skills install [--output <dir>]` | Installs these skills into the solution's `.agents/skills`. |
| `anarchy-toolkit boilerplate list\|show <path>\|diff [<path>...]` | Lists or prints the packaged Makefile, scripts and settings files, or diffs the solution's copies against them, without installing anything. |

- Without arguments, `bootstrap` and `scaffold` start an interactive wizard; only use it in an interactive terminal,
  otherwise ask the user for the names and pass them.
- `bootstrap` defaults to folder-based libraries (`src/Library/<Layer>`, apps under `src/Api`, `src/Jobs`, `src/Ui`);
  use `--layering prefix` for name-prefix layout. For custom layers, ask the user to run the wizard.
- After `bootstrap`: `cd <dir> && make install`. After `scaffold`: `make compile check_layering`, then review the new
  application's settings (auth URLs, client IDs) and run `make appsettings` for local ones.

## Guides

Two guides are bundled with this skill:

| Guide | Use it for |
|---|---|
| [mental-model](references/guides/mental-model.md) | Before the first change in an unfamiliar Anarchy solution: the pillars, what is the solution's and what is the toolkit's, and how a request or a job travels through the toolkit's types. |
| [greenfield-solution](references/guides/greenfield-solution.md) | Creating a solution: the layout choice, the order to add applications, mocks, company pieces and databases, and the checklist to finish with. |

Adoption guides ship with `anarchy-adopt`, conventions and decisions with `anarchy-conventions`. The feature guides
are online; read them when a task needs the detail:

| Feature | Guide |
|---|---|
| Layers, the toolkit's modules, the layering check, the dependency map | https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html |
| `Observer`, runtime sinks, a custom `ObserverFactory` | https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html |
| Persistence providers (PostgreSQL, MySQL, Oracle) | https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html |
| The mock server: expectations, own host, assertions, recording | https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html |
| Blazor UIs on `Web.Ui` | https://anarchy-io.github.io/anarchy-toolkit/guides/building-uis.html |
| Fallout components, test discovery, artifacts, releases | https://anarchy-io.github.io/anarchy-toolkit/guides/fallout-builds.html |
| Packages, one README each (also in the NuGet cache) | https://anarchy-io.github.io/anarchy-toolkit/packages/ |

The guides are written for people: walkthroughs, tables and diagrams. They are copied from the documentation site
at the toolkit version these skills were installed with, and their layout may change between versions. Read them for
**order, reasons and checks**; take **commands and parameters** from these skills and `make help`, which match the
installed build. When a guide and a skill disagree, the skill wins; tell the user. Look for these elements, whatever
the headings are called:

- **Steps in order**, often numbered or drawn as a flowchart: follow the order, one step per change.
- **Ground rules or checklists**: apply them to every change.
- **"Done when" or "Check" lines**: run them before calling a step finished.
- **Tables of symptom, cause and fix**: match the error you see.
- **Links to conventions and decisions**: load `anarchy-conventions` for them.

If a page you expect is missing, the published site (https://anarchy-io.github.io/anarchy-toolkit/guides/) has the
latest version; it may describe features newer than the installed toolkit, so check `make help` before relying on it.
