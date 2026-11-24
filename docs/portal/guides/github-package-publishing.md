# GitHub package publishing

This guide is for owners of the Anarchy Toolkit repository. Pushes to `main` and manual retries publish the versions
already committed to `main`. An explicitly requested manual release can also generate a version-and-notes commit with
the dedicated release App before publishing. Repository and environment configuration below is required; the workflow
alone cannot enforce GitHub rulesets or approvals.

## Configure the release identity and approval gate

1. Register a dedicated GitHub App and install it on this repository only. Grant repository **Contents: read and
   write**, plus GitHub's mandatory metadata read access. Do not grant workflow, administration, pull request or other
   permissions, or enable unnecessary webhooks.
2. Set repository Actions variables `RELEASE_APP_CLIENT_ID` to the App's client ID and `RELEASE_APP_BOT_LOGIN` to its
   exact bot login, `<app-slug>[bot]`. The workflow refuses a missing bot login and verifies it matches the minted token.
3. Create the `release-publish` Actions environment. Restrict its deployment branches to protected `main`; require
   independent, trusted release-maintainer reviewers, enable prevention of self-review, and disable administrator
   bypass of the environment protection rules. Do not include ordinary contributors as approvers. Protect changes to
   workflow files and environment configuration. If the GitHub plan or repository settings cannot enforce these controls,
   do not enable direct App commits; continue releasing through reviewed pull requests instead.
4. Store the App's PEM private key as the **environment secret** `RELEASE_APP_PRIVATE_KEY` in `release-publish`, not as a
   repository or organization secret. Restrict access to the key and rotate it if compromised. Only the protected
   environment should expose it; anyone who obtains it can act as the App outside this workflow.
5. Configure the existing `NUGET_USER` Actions secret and nuget.org trusted publishing for this repository and workflow,
   including its `release-publish` environment, with permission to publish the intended package IDs. The job exchanges
   its GitHub OIDC identity for a temporary NuGet API key; the GitHub App token is not used for NuGet authentication.

The job requests a current-repository installation token with only Contents write access. Installation tokens expire
after one hour and the token action revokes it at job completion. Checkout does not persist the token; the push step
uses it through the GitHub CLI credential helper. The job's `GITHUB_TOKEN` has only `id-token: write` for NuGet OIDC;
the recursion gate has no `GITHUB_TOKEN` permissions.

## Protect `main`

Configure two active rulesets targeting `main`:

1. **Review and checks:** require pull requests, the existing review requirements and required status checks. Add only
   the dedicated release App as a bypass actor, with the direct bypass needed for its release commit. Do not add
   contributors, administrators or broad roles as bypass actors. Preserve all existing review and check requirements.
2. **Branch integrity:** restrict deletion and block force pushes, with **no bypass actors**, including the App. Keep any
   other integrity restrictions that the App can satisfy in this no-bypass ruleset. Make sure legacy branch protection
   and other rulesets also enforce the intended protections without granting broader bypasses.

The App bypasses the review/check ruleset only for its direct release commit; it must never bypass the integrity ruleset.
Contributors must not bypass PR requirements, receive App credentials, or approve their own release runs. The environment
approval and branch restrictions above are essential: dispatch access alone must not become an indirect bypass. If GitHub
cannot enforce the separation and independent approval, use reviewed PRs instead of the automated commit path.

## Request and review a release commit

An authorized maintainer may dispatch the workflow **from `main`**, supplying all of:

- `release_base`: the full 40-character SHA of the chosen ancestor baseline.
- `release_head`: the full SHA of the reviewed current `main` commit. The job stops if `main` has moved by checkout;
  a non-fast-forward push also fails if it moves during generation.
- `release_bump`: `patch`, `minor` or `major`, and `confirm_release`: true.

Before approving the protected environment, an independent release maintainer must review the exact baseline/head range,
the bump classification, and a preview of the output in a clean disposable checkout using `make release_bump_changed`,
`make release_notes` and `make validate_release` with that baseline and head. Review the generated versions and notes,
including dependent bumps. The tooling skips projects already bumped above the baseline, but does not know whether a
package has ever been published or whether a particular change is breaking. Confirm that every proposed bump follows
repository release policy: never-published packages remain at their initial version, automation packages introduced or
renamed on the current branch remain at `1.0.0`, and published automation default changes require a major bump and
appropriate dependent handling. If a single bump choice cannot produce the correct result, prepare a reviewed PR instead.

As a conservative guard, the job rejects a baseline that predates any current source project; use a reviewed PR for
such a range. This guard does not identify all never-published packages, so the human review remains necessary. The job
generates changes with the release tooling, validates versions and runs `make pack` (including unit tests) before staging
only source project files and `RELEASE-NOTES.md` files. It commits and pushes only if those files changed; publication
starts only after a successful push. The App bypass means the generated commit itself does not go through a PR: the
independent preview and approval are required before it is created.

## Retries and serialization

- For a manual publish-only retry, leave both SHAs empty, select `none`, and leave confirmation unchecked. A normal
  push does not generate release changes. Publishing from another workflow ref is rejected.
- The `publish-nuget` concurrency group allows one run at a time and does not cancel the running job. GitHub may replace
  an older pending run with a newer one. A publish-only run checks out the latest `main`, not the triggering commit.
- A push attributed to the configured App bot skips the publishing job before environment approval, preventing a release
  commit from recursively publishing. Manual dispatch remains available. Update the bot login and credentials together
  if the App identity changes.
- Publishing is not transactional: a failed run may have already pushed some packages. Resolve the cause and manually
  retry without release inputs; existing NuGet versions are skipped. A failed push does not publish that release commit.
