# 0006. Fallout for the build, without deployment components

## Context

Every repository re-implemented compile, lint, test, pack and run targets, with small differences that made switching between repositories slow. The build started on [NUKE](https://github.com/nuke-build/nuke) components, which also carried AWS deployment flows (Lambda, [CodeDeploy](https://aws.amazon.com/codedeploy/), KMS, Slack) that were company-specific.

## Decision

Build automation ships as `Anarchy.Toolkit.Automation.Build`: reusable Anarchy components (`IStandardBuild`, `ILayeringBuild`, `IMockServerBuild`, `IDockerBuild`, ...) powered by [Fallout Build](https://fallout.build/) and composed in `build/Build.cs`, with a distributed Makefile and scripts as the developer entry point. Deployment components are left out: packages and container images are built and published; deploying them is left to each organization.

## Consequences

- `make compile`, `make test_unit`, `make pull_request` mean the same in every solution.
- Parameters (`.fallout/parameters.json`) and overridable members adapt targets without forking them.
- Teams that deploy with CodeDeploy or Lambda zips keep a separate release build, next to Fallout.

## Alternatives considered

- **Stay on NUKE.** Fallout continues its model; see the build's history.
- **Keep deployment components.** Rejected by the [Vendor-neutral value](README.md#values).
