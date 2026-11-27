# 0013. What a bootstrapped solution contains

## Context

The `anarchy-toolkit` CLI creates new solutions and adds applications to them. Earlier versions created a library for every layer, including an `Automation` library adopters never used, an empty `samples` folder, no tests, and namespaces that clashed between solutions (`Api.Orders`). Each adopter then fixed the same things by hand.

## Decision

A bootstrapped solution contains what most solutions keep, and nothing they delete:

- **Build host:** `build/_build.csproj` with its `build/Build.cs` entry file, added at the solution root; it shows `.fallout/parameters.json` in the solution view. Automation is customized there, never in a library.
- **Libraries:** one per layer except `Apps` (see [ADR 0001](0001-layered-architecture.md)), each referencing the one below it.
- **Applications:** in `src/Apps/<Apis|Jobs|Ui>/<Name>` (or `src/Apps.<Kind>.<Name>`), referencing the `Runtime` library, with shared settings in a monorepo ([ADR 0004](0004-validated-app-settings.md)).
- **Tests:** `tests/Unit`, `tests/Integration`, `tests/EndToEnd` and `tests/Common` by default, and optionally `tests/MockServer` ([ADR 0008](0008-test-levels.md)).
- **Namespaces:** every scaffolded project's root namespace starts with the solution name and follows its directory: `Shop.Apps.Apis.Orders`, `Shop.Library.Infrastructure`, `Shop.Tests.Unit`.
- **Solution items:** `scripts`, `Makefile`, `.anarchy-toolkit.json`, `global.json`, `nuget.config`, `Directory.Build.props`, `Directory.Packages.props` and `.editorconfig` appear under a `solution items` solution folder.
- **No `samples` folder.**

Every choice remains a wizard question or command-line flag (`--no-tests`, `--with-mock-server`, `--isolated-app-settings`).

## Consequences

- A new solution builds, tests and passes its layering check without edits.
- Namespaces are unique across solutions, so shared packages and generated code never collide.
- Bootstrapping writes more files; removing an unwanted test level means deleting a project.

## Alternatives considered

- **Minimal solutions, everything added on demand.** Rejected: the same projects were added to almost every solution, by hand and inconsistently.
- **Namespaces without the solution name (`Apps.Apis.Orders`).** Rejected: they collide as soon as two solutions share code or are loaded together.
