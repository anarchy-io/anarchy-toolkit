---
name: anarchy-conventions
description: "The coding conventions recommended for Anarchy Toolkit solutions, with the values and architectural decisions behind them: layering and where a file goes, dependency-injection modules and settings, async and cancellation, exceptions, enums, endpoints and DTOs, logging and metrics through Observer, and test layout and naming. Each one says how to opt out. Load it before writing or reviewing code in a solution built on the toolkit."
---

# Anarchy Toolkit conventions

How the toolkit recommends writing code in solutions built on it, and why. The pages under `references/` are copies
of the published conventions (https://anarchy-io.github.io/anarchy-toolkit/conventions/) and decisions, at the
version of the installed toolkit. They are written for people and their layout may change between versions; whatever
the headings, each convention has the rules, a reason (*Why*, linking a value or decision) and how to opt out with
what that costs. The published pages may be newer than the installed toolkit: prefer the bundled copies.

## When to use

- Before writing or reviewing code in an Anarchy solution: read the conventions the change touches.
- When unsure where a file goes, how to name a test, what to throw or how to log: open the matching page.
- When a convention seems wrong for the solution: read its *Why* and *Opting out or adapting* sections.

## Rules

- **The solution's own decisions win.** Conventions are recommendations. Before applying one, check the solution's
  `AGENTS.md`, ADRs (often `docs/adr/` or `docs/decisions/`) and existing code: where they deviate on purpose, follow
  them and don't "fix" the deviation.
- **Follow the surrounding code** when it is consistent, even where it differs from a convention; propose the
  convention to the user instead of mixing styles.
- **Opting out is a decision for the user.** Suggest an opt-out from the page's *Opting out or adapting* section when
  the *Why* doesn't apply, and record it where the solution keeps its decisions.
- Every page ends with what you lose by opting out: mention it when proposing one.

## Which page

| Working on | Read |
|---|---|
| Where a project, folder or file goes; layer folders; namespace collisions | [layering](references/conventions/layering.md) |
| Shared project layout, names, helpers, UseCases and aggregates | [code organization](references/conventions/code-organization.md) |
| Registering services, modules, settings | [dependency-injection](references/conventions/dependency-injection.md) |
| Controllers, Input/Output DTOs, `ActionResult<T>` | [endpoints](references/conventions/endpoints.md) |
| `async` methods, cancellation tokens, worker shutdowns | [async](references/conventions/async.md) |
| Throwing, catching, defining exception types | [exceptions](references/conventions/exceptions.md) |
| Enums, external value sets, auxiliary tables | [enums](references/conventions/enums.md) |
| Log messages, metric names and tags, `Observer` call sites | [observability](references/conventions/observability.md) |
| Test projects, naming, levels, kits, fixtures, mocks | [testing](references/conventions/testing.md) |
| An overview of all of them | [conventions index](references/conventions/README.md) |
| Why the toolkit is built this way | [values and decisions](references/decisions/README.md) |

How to apply the composition conventions step by step is in the `anarchy-compose` skill; layering checks and test
runs are in `anarchy-develop`.
