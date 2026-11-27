# 0012. Optional automation preferences and stable defaults

## Context

Build and scaffolding operations need useful defaults, but requiring adopters to create and maintain a configuration
file makes the tooling harder to adopt and breaks one-off commands and custom agent skills. At the same time,
solution-specific choices such as layout and test roots benefit from an explicit, reusable place. Fallout already
resolves invocation parameters from several sources, and the standalone CLI must remain useful without a Fallout
runtime.

## Decision

Use one optional solution-root file, `.anarchy-toolkit.json`, for persistable automation overrides. Every property is
optional; no file, `{}`, and partial sections are valid. Resolution is invocation-specific explicit arguments first,
then saved overrides, then in-code defaults or existing derived values. For Fallout, its existing command-line,
environment and parameter-file resolution remains ahead of saved overrides. The CLI has the same saved/default
fallback without depending on Fallout. No operation infers a layering mode from the repository layout.

Saving is opt-in and explicit. Normal builds do not write preferences; bootstrap and scaffold save only when asked,
with interactive consent defaulting to No. The one exception is a structural choice the generated code depends on:
bootstrap records `scaffolding.appSettings: shared` when it creates shared settings, so later scaffolds match the
solution instead of generating settings it does not use. Preference editing validates the merged document before an atomic write and
preserves unrelated values. The schema is a documentation/editor contract and does not materialize defaults into a
saved file. Credentials and transient or destructive-operation choices are not preferences.

Public automation defaults are part of the published package contract. Changing any public default is classified as a
breaking change and requires a major version bump for affected already-published packages plus dependent-package
handling through the release tooling. Packages introduced or renamed on the current branch remain at their initial
`1.0.0` version; their changes are still documented and classified as breaking rather than bumped prematurely.
Default behavior should be covered by a reviewed regression fixture so changes are intentional and reviewed as
contract changes.

## Consequences

- An adopter can run tools, builds and custom skills without a preferences file or a guessed layout.
- Teams can explicitly save stable choices, inspect saved values versus effective defaults, and clear individual
  overrides to restore fallback behavior.
- A setting has one owner and one root location; Fallout keeps its own engine files in `.fallout/`.
- Defaults require compatibility discipline: behavior changes to public automation defaults follow major-version and
  dependent-package release rules.

## Alternatives considered

- **Require a layering file.** Rejected: it turns optional automation into a setup prerequisite and blocks CLI use
  without persistence.
- **Infer layout from project names or folders.** Rejected: ambiguous repositories should receive explicit arguments
  or use the documented baseline, never a silent guess.
- **Store defaults in the file automatically.** Rejected: generated configuration obscures which values were chosen
  and makes defaults harder to evolve deliberately.
- **A separate settings directory or Fallout-owned preference file.** Rejected: one root file is easier to discover
  and can be consumed by the standalone CLI without coupling it to Fallout.
