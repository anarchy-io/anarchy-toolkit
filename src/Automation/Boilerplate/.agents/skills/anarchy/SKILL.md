---
name: anarchy
description: "Start here in any .NET solution built on Anarchy Toolkit (a Makefile, build/_build.csproj and Anarchy.Toolkit.* packages), or to create one: the anarchy-toolkit CLI (bootstrap a solution, scaffold APIs, jobs and UIs, install these skills), the solution layout and the rules, plus which anarchy-* skill to load for building, testing, running, releasing or maintaining it."
---

# Anarchy Toolkit

Anarchy Toolkit is a .NET 10 developer platform: layered solutions (Architecture), NuGet packages for persistence,
messaging, telemetry, hosting and web (Infrastructure), and Anarchy automation driven by `make`, powered by [Fallout Build](https://fallout.build/) (Build Ecosystem).
Docs: https://anarchy-io.github.io/anarchy-toolkit/

## When to use

- Creating a solution, or adding an API, job or UI to one: use the CLI below.
- Starting any work in an Anarchy solution: read the rules, then load the skill for the task.
- Adopting the toolkit in a repository: load `anarchy-adopt`.

## Rules

- Run `make` from the solution root. `make help` lists every target; `make help COMMAND=<target>` describes one and
  names the skill covering it. Prefer a target to hand-written `dotnet` commands: it encodes the solution's settings.
- Projects only reference their own or a lower layer (by default `Core < Infrastructure < Configuration < Hosting <
  Web < Automation`); `make check_layering` enforces it. Put new code in the lowest layer that can hold it.
- Before advising, scaffolding, building or checking architecture, read the optional solution-root
  `.anarchy-toolkit.json` and follow any `layering` overrides: its `mode` (`folder`, the baseline, or `prefix`),
  `layers`, `root`, and in folder mode `libraryRoot` and `applicationFolders`. With no override, use folder mode under
  `src/Library` with the standard app folders/layers. Never infer a mode from the repository. Explicit operation
  arguments win over saved preferences, which win over in-code defaults. A missing file or mode is valid.
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
| Where code goes, naming, UseCases and aggregates, async, exceptions, enums, endpoints, logging, tests: the conventions and why | `anarchy-conventions` |
| Set up, build, run, lint, test, check layering | `anarchy-develop` |
| Local Docker environment, mock server | `anarchy-environment` |
| Modules, settings, health reports, workers, telemetry (logging, metrics, correlation IDs, backends) | `anarchy-compose` |
| Adopting the toolkit, choosing packages, bringing tests, mocks or DI over; adoption pitfalls | `anarchy-adopt` |
| Versions, release notes, packages, container images | `anarchy-release` |
| Dependency upgrades, Git hooks, boilerplate, build targets, these skills | `anarchy-maintenance` |
| Targets the solution added itself | the solution's own skills in `.agents/skills` |

For adoption work, settle the path and scope with `anarchy-adopt`, then start a fresh session to carry out the plan; its
recipes and bundled references are deliberately loaded on demand.

## The anarchy-toolkit CLI

Install or update it with `dotnet tool update --global Anarchy.Toolkit.Automation.Build.Cli`.

| Command | What it does |
|---|---|
| `anarchy-toolkit bootstrap <Name> --output <dir> [--with-skills] [--layout monorepo\|single-app] [--application-folder <name>] [layout options] [--save-preferences]` | Creates a layered solution in an empty or new directory. `monorepo` keeps shared `src/Library`; `single-app` puts libraries under `src` and maps one application folder. Preference saving is opt-in. |
| `anarchy-toolkit scaffold api\|job\|ui <Name> [--layout monorepo\|single-app] [--application-folder <name>] [layout options] [--save-preferences]` | Adds a REST API (`Web`), job (`Hosting`) or Blazor UI (`Web.Ui`) using explicit, saved or default layout values. |
| `anarchy-toolkit scaffold library <Name> --layer <Layer> [layout options] [--dry-run] [--save-preferences]` | Adds a library to the effective folder or prefix layout, referencing the next lower layer when present. |
| `anarchy-toolkit scaffold tests [unit\|integration\|endtoend] [--dry-run] [--save-preferences]` | Creates the shared test project for a kind, honoring `TestKindAliases`; no kind starts an interactive choice. |
| `anarchy-toolkit scaffold api <Name> [--swagger-theme\|--no-swagger-theme]` | Explicitly enables or disables Swagger UI's theme for the generated API. |
| `anarchy-toolkit scaffold api\|ui <Name> [--container]` | Optionally generates a container web entry point. APIs also support `--lambda-api`. |
| `anarchy-toolkit scaffold job <Name> [--job-mode worker\|once] [--lambda-event]` | Selects a local worker (default) or one-shot job, optionally with a scheduled Lambda. |
| `anarchy-toolkit config show\|set\|unset\|configure` | Inspect effective preferences and their saved/default source, edit a typed JSON override, remove an override or use the interactive editor. |
| `anarchy-toolkit skills install [--output <dir>]` | Installs these skills into the solution's `.agents/skills`. |
| `anarchy-toolkit boilerplate list\|show <path>\|diff [<path>...]` | Lists or prints the packaged Makefile, scripts and settings files, or diffs the solution's copies against them, without installing anything. |

- Without arguments, `bootstrap` and `scaffold` start an interactive wizard; only use it in an interactive terminal,
  otherwise ask the user for the names and pass them.
- The bootstrap wizard asks one application or several before layering mode. `--layout monorepo` selects the shared
  `src/Library` baseline; `--layout single-app` maps one folder to `Web` for API/UI or `Hosting` for a job. It defaults
  to `App`; choose `Api`, `Job`, `Cli`, the solution name or another folder with `--application-folder`. Use
  `--layering prefix` for name-prefix layout. For custom layouts, use explicit operation arguments or ask the
  user whether to save selected preferences (`--save-preferences`; interactive consent defaults to No). Without
  saving, generation remains usable and reports layout arguments needed by later commands. No preferences are saved
  automatically. `config show` labels saved values versus in-code defaults; `ShowAnarchyPreferences` also labels active
  layering Fallout parameters when passed to the build. See https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html
  for what belongs in each settings file and their precedence.
- `scaffold library` and `scaffold tests` accept `--dry-run` to report without writing. `--save-preferences` only saves
  explicit layout choices; test-kind aliases come from Fallout's `TestKindAliases` or saved `tests.kindAliases`.
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
| `AppTelemetry`, logging and metrics providers, OTLP, optional Serilog, correlation IDs, Datadog and New Relic configuration | https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html |
| Persistence providers (PostgreSQL, MySQL, Oracle) | https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html |
| The mock server: expectations, own host, assertions, recording | https://anarchy-io.github.io/anarchy-toolkit/guides/mock-server.html |
| Blazor UIs on `Web.Ui` | https://anarchy-io.github.io/anarchy-toolkit/guides/building-uis.html |
| Anarchy automation components, test discovery, artifacts, releases | https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html |
| Packages, with a "Which package do I need?" table, one README each (also in the NuGet cache) | https://anarchy-io.github.io/anarchy-toolkit/packages/ |

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
