# Contributing Conventions

The rules for code in the Anarchy I/O Toolkit repository. Unlike the [conventions for adopters](../conventions/README.md), these are **required**: they are enforced by the build and in review, and a pull request that breaks one is not merged.

> **Process lives in [CONTRIBUTING.md](https://github.com/anarchy-io/anarchy-toolkit/blob/main/.github/CONTRIBUTING.md).** Branches, commit messages, history shaping and version bumps are described there, and it wins on any disagreement with this page. Coding agents get both through `AGENTS.md` and the `anarchy-contribute` skill.
{: .notice--warning}

## Every convention is a rule here

Each [adopter convention](../conventions/README.md) applies in full, without its *Opting out* section:

| Convention | Enforced by |
|---|---|
| [Layering](../conventions/layering.md) | `make check_layering` (CI), the [dependency map](../guides/architecture.md#dependency-map) |
| [Code Organization](../conventions/code-organization.md) | Review; existing names and folders are migrated selectively |
| [Dependency Injection](../conventions/dependency-injection.md) | Review |
| [Observability](../conventions/observability.md) | Review |
| [Async](../conventions/async.md) | Review |
| [Exceptions](../conventions/exceptions.md) | Review |
| [Enums](../conventions/enums.md) | Review |
| [Endpoints](../conventions/endpoints.md) | Review |
| [Testing](../conventions/testing.md) | Review; unit tests on every pull request |

## Rules that only apply to the toolkit

### Packages

- **Follow the [values](../decisions/README.md#values).** In particular: packages are domain-free (no business enums, entities or destinations), vendor-neutral (company specifics go behind extension points) and standalone (every dependency restores from nuget.org).
- **Test-only libraries stay in `Testing.*` kits.** No production project references [AwesomeAssertions](https://awesomeassertions.org/), [Moq](https://github.com/devlooped/moq) or xUnit.
- **One README per package.** `src/<Project>/README.md` describes the package and has a `## Releases` section with a note per published version. The copies under `docs/packages/` are generated.
- **New packages are listed** in the [Package Catalog](../packages/README.md), named after their layer (`<Layer>.<Name>`).
- **Versions are bumped with `make release_bump`**, never by hand, and validated with `make validate_release`.

### Build and code quality

- **The build that counts is Release with warnings as errors:** `dotnet build Anarchy.Toolkit.sln -c Release -p:TreatWarningsAsErrors=true`. Analyzer findings only show up there.
- **`make lint`** passes; `make lint_fix` applies the formatting.
- **Package versions** are centrally managed in [`Directory.Packages.props`](https://learn.microsoft.com/nuget/consume-packages/central-package-management); respect the compatibility notes in it.

### Tests

- Unit tests run on xUnit v3 and [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) in `tests/Unit/`, with folders mirroring the `src/` projects.
- Pass `TestContext.Current.CancellationToken` to calls that accept a token ([xUnit1051](https://xunit.net/xunit.analyzers/rules/xUnit1051)).

### Generated files and skills

- Regenerate and commit `docs/dependency-map.html` (`make dependency_map`) with any new project, project reference or package dependency.
- Commit `.fallout/build.schema.json` when the build regenerates it.
- [Make](https://www.gnu.org/software/make/) targets name their agent skill with `#@`; update the skill and run `make skills_sync`. `make skills_validate` runs in CI.

### Documentation

- Guides teach, conventions recommend, decisions explain. Put new pages in the matching section: [Guides](../guides/README.md), [Conventions](../conventions/README.md) or [Decisions](../decisions/README.md), and add them to `docs/_data/navigation.yml`.
- A convention page keeps the same shape: banner, why (linking to a value or ADR), the rules, *Opting out or adapting*.
- A choice that shapes the packages gets an ADR in `docs/decisions/`.
- Conventions, decisions and the onboarding guides also ship inside the agent skills (`make skills_docs_sync`, checked
  in CI). Agents read them without the site: keep links relative, keep the elements they look for (ordered steps, *Why*,
  *Opting out or adapting*, "Done when" checks, symptom/cause/fix tables), and update the skill's guidance when a
  page's shape changes.

## Checks before a pull request

```bash
make lint
make test_unit
make check_layering
make skills_validate   # when you changed make targets
```
