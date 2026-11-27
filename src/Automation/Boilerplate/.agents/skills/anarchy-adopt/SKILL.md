---
name: anarchy-adopt
description: "Plan first-time adoption of Anarchy Toolkit: choose between a new solution, an existing repository, or individual packages; set the order of work; find the detailed recipes for build setup, tests, modules, settings and telemetry."
---

# Adopt Anarchy Toolkit

Help the developer choose one adoption path, agree on the scope and sequence, then work through its recipe. Toolkit
docs: https://anarchy-io.github.io/anarchy-toolkit/.

## When to use

- Bringing a .NET repository onto Anarchy for the first time.
- Creating a new solution or adding individual toolkit packages.
- Choosing packages or planning how to plug in a company's logging, metrics, auth or deployment.

If the repository already has `build/_build.csproj` referencing `Anarchy.Toolkit.Automation.Build`, this is ongoing work,
not adoption: load `anarchy-develop` or `anarchy-maintenance` instead.

## Choose a path

Inspect the repository and optional root `.anarchy-toolkit.json`; do not infer a layering mode. Ask the developer if the
path or desired scope is ambiguous.

| Situation | Path and detailed recipe |
|---|---|
| Empty folder or only README/license/`.git` | [New solution](references/new-solution.md) |
| Existing .NET repository without the Anarchy build host | [Existing repository](references/existing-repository.md) |
| Application needs only one capability, not build tooling | [Individual packages and telemetry](references/packages-and-telemetry.md) |

## Plan in this order

1. Confirm the path, applications/capabilities and desired layering mode with the developer.
2. Read only the matching reference recipe above; load `anarchy-compose` for modules/settings/workers or
   `anarchy-environment` for Docker and the mock server when those topics arise.
3. Keep changes small and preserve existing behavior and pipeline contracts. Never overwrite files, move or rename
   projects, publish, push, or destroy data without the developer's approval.
4. Check the relevant targets with `make help`, use the toolkit CLI/build where available, and report what changed,
   what was skipped and the checks run.

**Context warning:** the adoption recipes and bundled guide references are substantial. Once the adoption plan is
settled, start a fresh session to carry it out; bring only the agreed plan and necessary context into that session.

## Shared rules

- Read and preserve existing work (`git status --short`) before edits. Generators refuse to overwrite; report conflicts
  instead of deleting files to make room.
- Install the agent skills with `anarchy-toolkit skills install` if the developer wants them; do not assume consent.
- Choose one layering mode for the whole solution. Folder mode is the baseline under `src/Library`, with standard
  application folders; prefix mode is explicit. Missing preferences are valid. Explicit arguments win over saved
  preferences, which win over code defaults. The bootstrap wizard asks whether there is one application or several;
  non-interactive setup can pass `--layout monorepo|single-app`, and `--application-folder` names the single app's
  folder. See [Automation Preferences](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html)
  for the owner and precedence of `.fallout/parameters.json`, `.anarchy-toolkit.json` and one-run arguments.
- Make and Bash are optional when running supported build targets directly: `./scripts/build.anarchy.ps1 <Target>` on Windows, or `dotnet run --project build/_build.csproj -- <Target> [--parameter value]`. See [Without Make](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html#without-make-including-windows).

## More help

- [Adopting an existing solution](references/guides/adopting-existing-solution.md): change order and constraints.
- [Onboarding recipes](references/guides/onboarding-recipes.md): tests, mocks, settings, modules, health and workers.
- [Pitfalls](references/guides/pitfalls.md): common surprises and fixes.
- [Bringing your company stack](references/guides/bringing-your-company-stack.md): organization-owned integrations.
- [Coming from NUKE or Cake](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html#coming-from-another-build-tool): migration examples and differences.
