# Contributing

Thank you for contributing! For a bug report, include the expected and actual behavior, a minimal reproduction, and your .NET SDK and operating system versions. For larger changes, open an issue first to discuss the approach.

## Getting started

Install the .NET 10 SDK (see `global.json` for the minimum version and roll-forward policy), Git, and Make. Clone the repository and restore/build the solution:

```bash
dotnet restore Anarchy.Toolkit.sln
dotnet build Anarchy.Toolkit.sln -c Release
```

Dependencies restore from nuget.org. See the [README](../README.md) for the Make and Fallout commands and the [guides](../docs/guides/README.md) for architecture, coding, and testing conventions.

## Making a change

1. Create a branch from the default branch and keep changes focused.
2. Follow the existing architecture and conventions. Add or update tests and documentation for changed behavior.
3. Run the unit tests and relevant checks:

   ```bash
   make test_unit
   make lint
   make check_layering
   ```

   Unit tests use xUnit v3 and Microsoft.Testing.Platform. They live in `tests/Unit/`, with folders mirroring the corresponding `src/` projects. The unit test command also runs on every pull request. Run additional tests relevant to your change when available.
4. Open a pull request describing the change, why it is needed, and how it was tested. Link any related issues and respond to review feedback.

NuGet dependencies are centrally managed in `Directory.Packages.props`. Review compatibility constraints in that file when updating packages; Dependabot proposes weekly grouped updates for NuGet and GitHub Actions.

## Branches and commit messages

Use scoped semantic commit titles in the form `type(scope): description`. Choose the type that best describes the change, such as `feat`, `fix`, `docs`, `test`, `build`, `ci`, `chore`, or `revert`, and use a scope that identifies the affected area. For example:

```text
feat(Build.Fallout): add configurable solution boilerplate
docs(contributing): document commit conventions
```

Keep the title plain text. The only formatting permitted in the title is apostrophes around the names of existing classes, objects, or files in the repository; do not use other formatting such as Markdown backticks or emphasis.

Additional context is optional. When included, separate it from the title with an empty line. The details may use Markdown formatting and must not exceed two paragraphs.

Prefer a branch name prefixed with the semantic group most relevant to the commits in the pull request. Include the GitHub issue number when available, followed by a brief snake_case description. For example, a pull request containing `feat`, `chore`, and `docs` commits could use `feature/#123/brief_snake_case_description` when feature work is the main purpose. Without an issue number, omit that segment.

## Publishing packages (maintainers)

Every push to `main` runs the NuGet workflow; it can also be started manually to retry publication. Before the first run, configure the GitHub Actions repository secret `NUGET_API_KEY` with a nuget.org API key authorized to push the `Anarchy.Toolkit.*` packages, including new package IDs if needed. Protect `main` and require reviewed pull requests and passing checks.

Each project under `src/` has its own `<Version>`. Prepare a release with `make release_bump RELEASE_PROJECT=Core.Events RELEASE_BUMP=patch` (or `minor`/`major`); it also bumps transitive package dependents. Review and commit the project files. PR checks compare versions with the base commit and reject missing or insufficient dependent bumps. They do not detect source changes that need a bump. Changes to a package with an existing version will **not** be published: nuget.org versions are immutable and the publisher skips duplicates.

After merging, the workflow runs unit tests, then uses the Fallout `Publish` target to pack all source projects, validate the batch, and push packages in dependency order. Every version not already on the feed is eligible, including on the first run. A failed push stops the remaining packages but cannot roll back those already pushed; fix the problem and rerun the workflow, which skips versions already on nuget.org. Feed indexing can delay availability. See the [NuGet build guide](../docs/guides/fallout-builds.md#nuget-packages) for details.
