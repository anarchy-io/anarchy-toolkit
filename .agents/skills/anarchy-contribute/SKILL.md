---
name: anarchy-contribute
description: "Contributing to the Anarchy Toolkit repository as an agent: the checks to run, branch and commit naming, shaping the history with fixup commits, bumping package versions with their release notes, regenerating the dependency map and build schema, and writing the pull request title and description. Use it whenever you change, commit or prepare a pull request in this repository."
---

# Contribute to Anarchy Toolkit

`.github/CONTRIBUTING.md` explains the process to people; this skill is the same process as steps for an agent. When
they disagree, CONTRIBUTING.md wins: report the difference.

## When to use

- Before committing anything in this repository.
- When a change touches a package that is published (anything under `src/`).
- When asked to prepare a pull request.

## Ground rules

- Never stage, revert or discard changes you did not make: the working tree may hold the developer's work in
  progress. Stage files explicitly (`git add <paths>`), never `git add -A` or `git commit -a`.
- Follow `docs/contributing/README.md` and the conventions in `docs/conventions/` (listed in `AGENTS.md`); in this
  repository they are rules, not recommendations. A choice that shapes the packages gets an ADR in `docs/decisions/`.
- Changing any public automation in-code default is a breaking change. For already-published packages, make the
  appropriate major version bump and use release tooling to handle dependent packages. Automation packages introduced
  or renamed on this branch remain at initial `1.0.0`; do not bump them, but document and classify the change as
  breaking.

## Before committing

1. `dotnet build Anarchy.Toolkit.sln -c Release -p:TreatWarningsAsErrors=true`: analyzer findings only show here.
2. `make test_unit lint check_layering`. Fix `lint` with `make lint_fix` and review the diff.
3. Projects, references or packages changed: `make check_package_hierarchy dependency_map`, and commit
   `docs/dependency-map.html`.
4. Fallout parameters or targets changed: commit the regenerated `.fallout/build.schema.json`.
5. Make targets changed: update the hand-written part of their skill, then `make skills_sync skills_validate`.

## Branch and commits

- Branch from `main`: `<group>/[#<issue>/]<snake_case_description>`, e.g. `feature/#123/agent_skills` or `fix/banner`.
- Commit titles: `type(scope): Description`, starting the description with an uppercase letter (`feat`, `fix`, `docs`,
  `test`, `build`, `ci`, `chore`, `refactor`, `revert`; `!` before the colon for breaking changes). Plain text only:
  apostrophes around existing class or file names are the only formatting allowed. An optional body, after a blank line,
  may use Markdown, up to two paragraphs.
- At least one commit per project introduced and per layer touched, each scoped to it.
- Fixes to work still on the branch (lint, analyzer findings, review feedback) are folded into the commit that
  introduced the code, never added as new commits:

  ```bash
  git commit --fixup=<commit>
  GIT_SEQUENCE_EDITOR=: git rebase -i --autosquash main
  ```

## Versions and release notes

For each package changed on purpose (not for repository-only changes such as `build/`, `docs/` or `.github/`):

1. `make release_bump RELEASE_PROJECT=<Project> RELEASE_BUMP=patch|minor|major`, once per change, from the versions on
   `main`; it bumps dependents too. Prefer bumping the lowest changed package when that already gives the others the
   bump they need. A package never published stays at its initial version.
2. Commit the changes, then `make release_notes RELEASE_BASE=main`; review the notes it adds under `## Releases`.
3. `make validate_release RELEASE_BASE=main` must pass.
4. Fold the version and note changes into the commits that need them.

## Pull request

Once everything is committed, write a title (same rules as commit titles, usually the main `feat` or `fix`) and a
description following `.github/pull_request_template.md`: what and why, how it was tested, and the checklist with each
item ticked or explained. Give it ready to paste; do not push or open the pull request unless asked.
