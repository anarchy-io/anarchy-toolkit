# Agent instructions

Anarchy.Toolkit is a .NET developer platform built around three pillars (Architecture, Infrastructure and Build
Ecosystem), delivered from this .NET 10 monorepo as independently published NuGet packages (`src/`), with unit tests
(`tests/Unit/`), sample applications (`samples/`), an Anarchy build powered by Fallout (`build/`) and a documentation site (`docs/`).
@README.md has the overview.

## Rules

- Never stage, revert or discard changes you did not make: the working tree may hold the developer's own work in
  progress.
- Load the `anarchy-contribute` skill before committing: it has the checks, commit and history rules, version bumps
  and the pull request description. @.github/CONTRIBUTING.md is the human version and wins on any disagreement.
- Once all local changes are committed, write the pull request title and description, ready to paste.
- Changing any public automation in-code default is a breaking change: require the appropriate major published-package
  version bump and dependent-package handling through the release tooling. Automation packages introduced or renamed
  on this branch remain at their initial `1.0.0`; document the breaking classification without bumping them.

## Skills

`.agents/skills` holds the skills for this repository:

- `anarchy`, `anarchy-adopt`, `anarchy-compose`, `anarchy-conventions`, `anarchy-develop`, `anarchy-environment`, `anarchy-release`,
  `anarchy-maintenance`: the toolkit's distributed skills, linked from `src/Automation/Boilerplate/.agents/skills`.
  Edit them there; they ship to users.
- `anarchy-conventions`, `anarchy-adopt` and `anarchy` copy pages from `docs/` into their `references/` (conventions,
  decisions, onboarding guides): edit the docs, then run `make skills_docs_sync` (CI checks it).
- `anarchy-contribute`: how to contribute to this repository.
- `anarchy-monorepo`: this repository's own make targets (CLI from source, documentation site, API reference).

Make targets name their skill with `#@`; `make skills_validate` (run in CI) checks that the skills list them.

## Where code goes

- @docs/guides/architecture.md: the layers (`Core < Infrastructure < Configuration < Hosting < Web < Automation`), project
  structure and DI modules. Projects only reference lower layers; `make check_layering` enforces it. Within a layer,
  this monorepo's packages also follow their name hierarchy (a package may reference its name ancestors, any lower
  layer, or a pair listed with its reason in `build/PackageHierarchy.cs`); `make check_package_hierarchy` enforces it
  for this repository only, never for solutions that use folder-mode layering.
- @docs/guides/automation-builds.md: the `Automation.Build` components, targets and release process.
- @docs/guides/automation-preferences.md: the optional root `.anarchy-toolkit.json`, settings inventory, precedence
  and explicit editing/saving flows. No preference file is required; layering defaults to folder mode under
  `src/Library` with standard application directories and layers, and the toolkit never guesses a mode.
- @docs/packages/README.md: the package catalog; add new packages to it. Each package has its own `src/<Project>/README.md`
  with a `## Releases` section. The copies under `docs/packages/<Project>/` are generated and git-ignored.
- @samples/README.md: the sample applications; samples are not packed and are ignored by the layering check.
- The distributed Makefile, scripts and skills live in `src/Automation/Boilerplate/`; this repository's @Makefile
  includes them in place.

## Conventions

@docs/contributing/README.md: the rules for this repository. Every convention below is required here (the
"Opting out" sections only apply to adopters):

- @docs/conventions/layering.md
- @docs/conventions/code-organization.md
- @docs/conventions/naming.md
- @docs/conventions/usecases-and-aggregates.md
- @docs/conventions/dependency-injection.md
- @docs/conventions/async.md
- @docs/conventions/exceptions.md
- @docs/conventions/enums.md
- @docs/conventions/observability.md
- @docs/conventions/endpoints.md
- @docs/conventions/testing.md

Feature guides: @docs/guides/observability.md, @docs/guides/building-uis.md, @docs/guides/database-providers.md,
@docs/guides/mock-server.md. @docs/guides/README.md summarises each guide; @docs/decisions/README.md lists the values
and ADRs.

## Practical notes

- The build that matters is Release with warnings as errors:
  `dotnet build Anarchy.Toolkit.sln -c Release -p:TreatWarningsAsErrors=true`. Analyzer findings (e.g. xUnit1051: pass
  `TestContext.Current.CancellationToken` to calls that accept a token) only show up there.
- Unit tests run on Microsoft.Testing.Platform: `cd tests/Unit && dotnet run -c Release --no-build -- -class "*MyTest"`
  (or `-namespace "…"`). Their folders mirror the `src/` projects.
- Package versions are centrally managed in @Directory.Packages.props; respect the compatibility notes in it.
- Build targets that need the build's runnable projects only consider `Exe` projects under `src/`.
