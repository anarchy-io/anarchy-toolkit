# Contributing

Thank you for contributing! For a bug report, include the expected and actual behavior, a minimal reproduction, and your .NET SDK and operating system versions. For larger changes, open an issue first to discuss the approach.

## Getting started

Install the .NET 10 SDK (see `global.json` for the minimum version and roll-forward policy), Git, and Make. Clone the repository and restore/build the solution:

```bash
dotnet restore Anarchy.Toolkit.sln
dotnet build Anarchy.Toolkit.sln -c Release
```

Run `make help` for every target, with its parameters. Dependencies restore from nuget.org. See the [README](../README.md) for the Make and Anarchy commands. The [conventions](../docs/conventions/README.md) are recommendations for applications built on the platform, but rules for contributions to this repository: [Contributing Conventions](../docs/contributing/README.md) lists them with the rules that only apply here. Choices that shape the packages are recorded in [Values and Decisions](../docs/decisions/README.md).

## Making a change

1. Create a branch from the default branch and keep changes focused.
2. Follow the existing architecture and conventions. Add or update tests and documentation for changed behavior.
3. Run the unit tests and relevant checks:

   ```bash
   make test_unit
   make lint
   make check_layering
   make check_package_hierarchy
   make skills_validate   # when you changed make targets
   ```

   Unit tests use xUnit v3 and Microsoft.Testing.Platform. They live in `tests/Unit/`, with folders mirroring the corresponding `src/` projects; tests that drive real tools (Git, the .NET SDK) live in `tests/Integration/`, and those that run a user-facing entry point (the CLI, the packaged Makefile and scripts) in `tests/EndToEnd/`. The unit test command also runs on every pull request. Run additional tests relevant to your change when available.
4. Open a pull request describing the change, why it is needed, and how it was tested. Link any related issues and respond to review feedback.

NuGet dependencies are centrally managed in `Directory.Packages.props`. Review compatibility constraints in that file when updating packages; Dependabot proposes weekly grouped updates for NuGet and GitHub Actions.

When you add or change a project reference between packages of the same layer, run `make check_package_hierarchy`; this monorepo-only check does not apply to adopter solutions. Whenever you add a project, a project reference or a package dependency, run `make dependency_map` and commit the regenerated `docs/dependency-map.html` together with the change. Likewise, commit `.fallout/build.schema.json` when the build regenerates it after you add or change Fallout parameters.

Make targets declare the agent skill that documents them with a `#@` comment. When you add or change one, update the written part of that skill (`src/Automation/Boilerplate/.agents/skills` for the distributed targets, `.agents/skills/anarchy-monorepo` for this repository's), then run `make skills_sync` to refresh its generated command table.

## Working with coding agents

Agents are welcome. [`AGENTS.md`](../AGENTS.md) and the skills in `.agents/skills` give them this guide as step-by-step instructions (`anarchy-contribute`), so you can ask an agent to prepare commits and a pull request description that follow it. You stay responsible for what you submit: review the agent's changes and history as you would your own.

## Branches and commit messages

Use scoped semantic commit titles in the form `type(scope): Description`, starting the description with an uppercase letter. Choose the type that best describes the change, such as `feat`, `fix`, `docs`, `test`, `build`, `ci`, `chore`, or `revert`, and use a scope that identifies the affected area. For example:

```text
feat(Automation.Build): Add configurable solution boilerplate
docs(contributing): Document commit conventions
chore(release): Bump affected packages
```

Keep the title plain text. The only formatting permitted in the title is apostrophes around the names of existing classes, objects, or files in the repository; do not use other formatting such as Markdown backticks or emphasis.

Additional context is optional. When included, separate it from the title with an empty line. The details may use Markdown formatting and must not exceed two paragraphs.

Shape the history for review:

- Commit at least once per project introduced and per layer touched, so each commit stays focused on one area and its scope is accurate.
- Keep follow-up fixes to work that is still on the branch out of the history: fold them into the commit that introduced the code (`git commit --fixup=<commit>`, then `git rebase -i --autosquash <base>`). This includes formatting fixes from `make lint_fix` and analyzer fixes found by the Release build.
- Fold version bumps into the commit of the change that needs them, once `make validate_release` passes (see [Publishing packages](#publishing-packages-maintainers)).

Prefer a branch name prefixed with the semantic group most relevant to the commits in the pull request. Include the GitHub issue number when available, followed by a brief snake_case description. For example, a pull request containing `feat`, `chore`, and `docs` commits could use `feature/#123/brief_snake_case_description` when feature work is the main purpose. Without an issue number, omit that segment.

## Publishing packages (maintainers)

Every push to `main` runs the NuGet workflow; it can also be started manually to retry publication. Before the first run, configure the GitHub Actions repository secret `NUGET_API_KEY` with a nuget.org API key authorized to push the `Anarchy.Toolkit.*` packages, including new package IDs if needed. Protect `main` and require reviewed pull requests and passing checks.

Each project under `src/` has its own `<Version>`. Prepare a release with `make release_bump RELEASE_PROJECT=Core.Events RELEASE_BUMP=patch` (or `minor`/`major`); it also bumps transitive package dependents. Review and commit the project files. PR checks compare versions with the base commit and reject missing or insufficient dependent bumps. They do not detect source changes that need a bump. Changes to a package with an existing version will **not** be published: nuget.org versions are immutable and the publisher skips duplicates.

When bumping versions:

- Never edit `<Version>` by hand; always use `make release_bump`, so dependents are bumped too.
- Bump from the base branch's versions, and run the target once per change: it bumps from the current working versions, so repeated runs stack. When several changed projects depend on each other, bump the lowest one only if that already brings the others to the version they need, as dependents get the same kind of bump.
- Keep a package that has never been published at its initial version (e.g. `1.0.0`), even if the target bumped it as a dependent.
- Changing any public automation in-code default is a breaking change, even if the implementation is otherwise
  additive. For already-published packages, use the appropriate major bump and the release tooling to handle dependent
  packages. Automation packages introduced or renamed on this branch stay at their initial `1.0.0`; do not bump those
  packages, but still document and classify the default change as breaking.
- Add a release note under `## Releases` in the README of each package changed on purpose. `make release_notes RELEASE_BASE=main` generates the missing ones from the subjects of the commits affecting each package, dependents included, for its current version. It only adds entries, leaving versions already listed untouched, so run it after the changes are committed and the versions bumped, then review the notes and fold them into the relevant commits.
- Check the result with `make validate_release RELEASE_BASE=main` before committing.

After merging, the workflow runs unit tests, then uses the Anarchy `Publish` target to pack all source projects, validate the batch, and push packages in dependency order. Every version not already on the feed is eligible, including on the first run. A failed push stops the remaining packages but cannot roll back those already pushed; fix the problem and rerun the workflow, which skips versions already on nuget.org. Feed indexing can delay availability. See the [NuGet build guide](../docs/guides/automation-builds.md#nuget-packages) for details.
