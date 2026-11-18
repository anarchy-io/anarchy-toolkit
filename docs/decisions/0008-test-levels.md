# 0008. Three test levels, one project per level

## Context

Repositories named their test projects differently (`Tests`, `Acceptance`, `Specs`, one per production project), so a build could not tell which tests need containers or a running mock server, and delivery ran everything or nothing.

## Decision

Tests are `Unit` (no I/O), `Integration` (one adapter against a real or mocked dependency) and `EndToEnd` (a whole application in process), each in its own project, matched by name segment. `Pack` and container publishing run only the unit tests. Other names map through `TestKindAliases`, or `IsTestProjectOfKind` can be overridden.

## Consequences

- `make test_unit`, `test_integration` and `test_end_to_end` run the right projects with the right dependencies.
- Delivery stays fast and independent of containers.
- Repositories with other names configure aliases or rename.

## Alternatives considered

- **[Traits/categories](https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests) inside one project.** Hides the dependencies a level needs and makes per-level runs slower.
