# Agent instructions

Anarchy.Toolkit is a .NET 10 monorepo of NuGet packages (`src/`), with unit tests (`tests/Unit/`), sample applications
(`samples/`), a Fallout build (`build/`) and a documentation site (`docs/`). Read @README.md for the overview and the
Make and Fallout commands.

## Rules

Follow @.github/CONTRIBUTING.md. It is the source of truth for:

- checks to run before committing (`make test_unit`, `make lint`, `make check_layering`);
- branch names, commit titles and how to shape the history (one commit per project introduced or layer touched;
  follow-up fixes folded into the commit they fix);
- version bumps, which only ever go through `make release_bump` and are checked with `ValidateLocalRelease`;
- regenerating `docs/dependency-map.html` with `make dependency_map` when projects, references or packages change.

Never stage or revert changes you did not make: the working tree may hold the developer's own work in progress.

## Where code goes

- @docs/guides/architecture.md: the layers (`Core < Infrastructure < Configuration < Hosting < Web < Build`), project
  structure and DI modules. Projects only reference lower layers; `make check_layering` enforces it.
- @docs/guides/fallout-builds.md: the `Build.Fallout` components, targets and release process.
- @docs/packages/README.md: the package catalog; add new packages to it. Each package has its own `src/<Project>/README.md`
  with a `## Releases` section. The copies under `docs/packages/<Project>/` are generated and git-ignored.
- @samples/README.md: the sample applications; samples are not packed and are ignored by the layering check.

## Conventions

- @docs/guides/async-conventions.md
- @docs/guides/exception-conventions.md
- @docs/guides/enum-conventions.md
- @docs/guides/observability.md
- @docs/guides/exposing-endpoints.md
- @docs/guides/building-uis.md
- @docs/guides/database-providers.md
- @docs/guides/testing-conventions.md

@docs/guides/README.md summarises each guide.

## Practical notes

- The build that matters is Release with warnings as errors:
  `dotnet build Anarchy.Toolkit.sln -c Release -p:TreatWarningsAsErrors=true`. Analyzer findings (e.g. xUnit1051: pass
  `TestContext.Current.CancellationToken` to calls that accept a token) only show up there.
- Unit tests run on Microsoft.Testing.Platform: `cd tests/Unit && dotnet run -c Release --no-build -- -class "*MyTest"`
  (or `-namespace "…"`). Their folders mirror the `src/` projects.
- Package versions are centrally managed in @Directory.Packages.props; respect the compatibility notes in it.
- The distributed Makefile and scripts live in `src/Build.Cli/Boilerplate/`; this repository's @Makefile includes them.
- Fallout targets that need the build's runnable projects only consider `Exe` projects under `src/`.
