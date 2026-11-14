# Fallout Builds

`Anarchy.Toolkit.Build.Fallout` (`src/Build.Fallout`) holds the toolkit's [Fallout](https://fallout.build/) build components. They are packaged as build components so every repository gets the same targets. The toolkit's own `./build` is its first consumer. 🔧

## Using it

A build project is a `partial` class that implements the components it needs; anything specific to the repository goes in its own `Build.*.cs` files:

```csharp
using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;
using Fallout.Common.CI;
using Fallout.Common.Tools.DotNet;

namespace Build;

[ShutdownDotNetAfterServerBuild]
[DotNetVerbosityMapping]
internal sealed partial class Build : FalloutBuild, IStandardBuild, ILayeringBuild, IContainerBuild
{
    public IReadOnlyCollection<Artifact> Artifacts => [ /* see "Artifacts" */ ];

    public string ContainerRegistry => "123456789012.dkr.ecr.eu-west-1.amazonaws.com";

    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
}
```

Reference the package (`<PackageReference Include="Anarchy.Toolkit.Build.Fallout" />`) from `build/_build.csproj`. The package carries a `buildTransitive` targets file that selects compatible `NuGet.*` assemblies from the installed SDK where appropriate. A `ProjectReference`, as the toolkit's own build uses, does not import it automatically: import `buildTransitive/Anarchy.Toolkit.Build.Fallout.targets` from `build/Directory.Build.targets`.

## Components

| Component | Targets | Notes |
|---|---|---|
| `IDefaultBuild` | `Clean`, `Build` | The base of every component: solution and directory conventions, `Artifacts`, `TestFilter`/`TestingPlatformCommandLineArguments`. |
| `IVersionedBuild` | — | Adds the `--build-number` parameter. |
| `IStandardBuild` | `Restore`, `Compile`, `TestUnit`, `TestIntegration`, `TestEndToEnd`, `TestAll`, `Lint`, `LintFix`, `ListProjects`, `RunApp`, `RunApps`, `GenerateAppSettings`, `PullRequest` (`Lint`, `CheckLayering` when `ILayeringBuild` is implemented, and `TestAll`) | Overrides `TestsDirectory` (`tests`) and `IsTestProject` (ignores test kits under `src`). |
| `INuGetBuild` | `ListPackages`, `GenerateLocalRelease`, `ValidateLocalRelease`, `Pack`, `Publish` | Versions source projects, and packs and publishes the batch in package dependency order. |
| `IContainerBuild` | `EcrPublishAppArtifact`, `EcrImageBuild`, `EcrImagePush` | ECR flow for `--app-name`. Implement `ContainerRegistry`; image name, tag (short commit SHA), Dockerfile (`docker/ecs/Dockerfile`) and runtime have defaults. |
| `IDependencyMapBuild` | `GenerateDependencyMap` | Writes a static HTML page (`--dependency-map-output`, default `docs/dependency-map.html`): a Mermaid graph of project references with layering violations highlighted, an onion of the layers (click a ring, or a group outside the chain, to zoom in on the references between its own projects; `#<Group>` in the URL opens it directly) and a table of projects, files and lines per group. Override `DependencyMapLayers` and `GetDependencyMapGroup` for other naming conventions. It carries no timestamp, so CI can regenerate it and commit only when it changed. |
| `IBoilerplateBuild` | `SetupBoilerplate` | Creates a starter solution, folders, Makefile, scripts and configuration. Select or exclude individual items with `--boilerplate-items` / `--boilerplate-exclude`; `--append-existing` optionally appends the packaged Git and editor settings and adds the packaged test runner to an existing `global.json`. See the [package README](../../src/Build.Fallout/README.md#choosing-what-to-install) for setup. |
| `ILayeringBuild` | `CheckLayering` | Fails when layering is broken. `--layering-root` selects the solution-relative (or absolute) project root, defaulting to `src/`; `--layering-by-folder true` uses its first folder instead of project-name prefixes, with no fallback (see the [architecture guide](architecture.md#enforcing-the-rule)). Override `LayeringLayers` and `GetLayer` (return `null` to leave a project out) for other conventions. |
| `IDependencyUpgradeBuild` | `UpgradeDependencies` | Lists outdated packages and optionally runs the global `dotnet-outdated-tool`. |
| `IGitHooksBuild` | `EnableGitHook`, `DisableGitHook`, `ShowGitHooks` | Manages local hooks; generated hooks invoke the repository's Make targets. |

Run `make fallout_help` for the full list of targets and parameters, or `make fallout TARGET="--plan"` for the execution plan.

### Local maintenance

`LintFix --auto-stage-lint-fixes true` (or `make lint_fix AUTO_STAGE=true`) re-stages only files that were already staged and had no unstaged edits before formatting. Other changes remain unstaged. It skips a file if its index entry changes during formatting; do not edit files concurrently. Without the option, linting never stages changes.

`UpgradeDependencies --dependency-version-lock Minor` lists outdated packages and asks before installing/reinstalling `dotnet-outdated-tool` and before upgrading. In CI or with redirected input, neither operation occurs without the explicit `--dependency-install-tool true` and `--dependency-upgrade true` flags. The Make equivalents are `INSTALL_TOOL=true` and `UPGRADE=true`. The upgrade first updates matching literal, unconditional root central package versions, including transitive pins, then upgrades direct references and verifies the solution restore. Explicit ranges, conditional versions and nested central files may need manual attention if restore fails. A failed operation may leave partial changes; review them before committing. The global tool and NuGet feeds must be available.

`EnableGitHook --hook-dry-run true --hook-skip-test-runtime true` previews a hook; omit dry-run to install. It respects Git's configured hooks directory and refuses to replace an existing hook unless `--hook-force true` is supplied. Options also skip lint fixing or static tests. `ShowGitHooks` and `DisableGitHook` accept `--hook-type`; the defaults are all and `pre-commit`, respectively. The generated hook calls Make and requires the corresponding Fallout components in the host build. SDK installation itself remains outside Fallout.

### NuGet packages

Implement `INuGetBuild` to pack projects under `src/`, including projects not yet in the solution (override `NuGetProjects` to select others). `Pack` compiles the solution and writes packages and symbols into a fresh `artifacts/packages/<run-id>/` directory; each project's MSBuild packaging settings determine its metadata, version, packability and symbol format. `Publish` runs `Pack` first, validates the whole batch and pushes packages sequentially, with every dependency in the batch before its consumers, including dependencies within the same layer and across target frameworks. Cycles, duplicate package IDs and incompatible in-batch dependency versions stop publication before the first push. Dependencies outside the batch must already exist on the feed. A failed push stops the rest; already pushed packages cannot be rolled back.

For example, `make publish SOURCE=https://api.nuget.org/v3/index.json` uses configured credentials, or supply `API_KEY` (prefer a secret environment variable in CI). Duplicate versions are skipped by default; set `--skip-duplicate false` on the Fallout target to fail instead. `dotnet nuget push` handles matching `.snupkg` files alongside packages when the feed supports symbols. A successful push does not guarantee immediate availability on feeds with indexing delays.

To prepare a release locally, run `make release_bump RELEASE_PROJECT=Core.Events RELEASE_BUMP=minor` (also `major` or `patch`). It increments the selected project's own version and every transitive source-project dependent by the same kind of increment, once each, including test kits. It uses project references, not layer names, and does not commit or publish. Projects must have one explicit unconditional stable `major.minor.patch` version; prereleases and computed versions are not supported. Review and commit the resulting project files with the changes.

`make fallout TARGET="ValidateLocalRelease --release-base origin/main"` compares current project versions with a Git commit or ref. For every existing package whose version increased, all existing transitive dependents must have at least the same severity bump (major > minor > patch); decreases fail. New projects have no baseline and are excluded from this comparison. The PR workflow compares against the PR base commit with full Git history. This checks propagation of version changes, **not** whether source changes themselves require a version bump.

#### Automatic releases

Pushes to `main` run change detection, bump affected packages, add missing README release notes, run checks, commit the versions and notes, and publish in the same workflow. The default bump is patch. Source-project changes propagate to all transitive package dependents, once each. Shared build settings affect all packages. Changes in tests, samples and build tooling do not normally cause a release; `Build.Cli` is an exception for the root dotfiles it embeds (its Makefile and scripts live in its own `Boilerplate/` folder), and its dependents (`Build.Fallout`, `Tool`) follow. Central package version changes use restored dependency graphs, including transitive packages. Removal of a central pin, conditional entries, and changes to other central settings conservatively affect all packages because the previous restore graph is not available. Missing or invalid restored assets fail rather than silently missing consumers.

For a local comparison, check out the head commit with clean source/build settings and run:

```sh
make release_bump_changed RELEASE_BASE=origin/main RELEASE_HEAD=HEAD RELEASE_BUMP=patch
# or: make fallout TARGET="GenerateReleaseFromChanges --release-base origin/main --release-head HEAD --release-bump minor"
```

The head defaults to `HEAD` and must match the checkout; arbitrary commit pairs can be compared by checking out the requested head first. Versions already increased relative to the baseline are retained, so a manual minor or major bump can be committed with a PR instead of receiving an additional patch bump. Existing version propagation rules are validated before writing. New packages are bumped from their declared initial version.

After bumping, run `make release_notes RELEASE_BASE=origin/main RELEASE_HEAD=HEAD` to fill missing `### <version>` entries in affected packages' README `## Releases` sections. The baseline must be an ancestor of the checked-out head. The target uses commit subjects from that range, matched to affected projects and their transitive dependents using the current project graph. Merge commits are compared with their first parent; their subjects and those of commits on the merged branch may both appear. Existing entries are never overwritten, and missing READMEs or Releases sections are created. Review generated notes before committing. A package with no attributable nonempty commit subject fails the target rather than generating an empty entry.

The workflow checks out the latest `main` and uses the latest automation release commit as its checkpoint, falling back to the push's previous commit on the first run. It records a checkpoint for each comparison, even an empty commit when nothing needs a bump. This lets a later run pick up changes when GitHub replaces a pending concurrency run and prevents a manual bump from covering subsequent changes. A competing push can cause the version-commit push to fail; the newer push's run processes the updated `main`. Force-pushes, a missing baseline, or branch protection that prevents the bot from pushing require manual intervention. Configure GitHub Actions with contents-write access, allow the bot to push through branch protection, and configure NuGet trusted publishing and `NUGET_USER`.

If publishing fails after the version commit, use **Run workflow** without a baseline to retry publishing the current versions; duplicates are skipped. To explicitly generate bumps with a different severity, use **Run workflow** with `release_base` and `release_bump`. Every publish attempts all packages in dependency order, so missing versions can be recovered; a partial NuGet publication cannot be rolled back. Do not retry an old run with a manually selected baseline unless another bump is intended.

### How `IStandardBuild` finds test projects

`TestUnit`, `TestIntegration` and `TestEndToEnd` run the test projects of their kind. A project is of a kind when its name *is* the kind (a single `Unit` project) or when one of its dot-separated segments is the kind and another is `Tests`: `Tests.Unit.Core.Primitives`, `MyService.Unit.Tests` and `MyService.Tests.Unit` are all unit test projects. Override `IsTestProjectOfKind` to change that. When `Compile` ran first, the tests skip restoring and building.

### Local `appsettings.Development.json`

`GenerateAppSettings` (`make appsettings` in this repo) writes the untracked `appsettings.Development.json` next to every tracked `appsettings.json.template` (override `AppSettingsTemplates` to choose which). The template holds only the keys a developer must override from the shared `appsettings.json`, and each value is a short explanation of what is expected and, optionally, where to find it:

```json
{
  "AuthService": {
    "Host": "Base URL of the auth service you develop against, ending with '/' (e.g. https://auth.dev.example.local/)",
    "ClientId": "OAuth client id registered for your local app, see the auth service's client registry"
  }
}
```

Each key not yet set is prompted for, showing its explanation; values already set and keys that are not in the template are kept, so re-run it whenever the template gains a key. A blank answer leaves the key out (an explanation is never written as a value) and it is reported as missing. `--reset-app-settings` (`RESET=true`) asks for every key again, offering the current value as default. Non-string template values are fixed defaults, copied when missing; keys named like a secret (`Password`, `Secret`, `Token`, `ApiKey`) are read with masked input. Without an interactive console (CI, redirected input) nothing is prompted and the missing keys are only reported. Keep `appsettings.Development.json` in `.gitignore`.

## Artifacts

`Artifacts` lists the deployable applications. A repository that only ships packages (like the toolkit) returns `[]`.

```csharp
public IReadOnlyCollection<Artifact> Artifacts => [
    new() { ProjectFile = this.As<IDefaultBuild>().SourceDirectory / "Api" / "Orders" / "Orders.csproj" },
    new() { ProjectFile = this.As<IDefaultBuild>().SourceDirectory / "Jobs" / "Sync" / "Sync.csproj" },
];
```

## Test runners

Projects on Microsoft.Testing.Platform (every `xunit.v3` project) run through the MSBuild `Test` target the platform injects; everything else (xUnit.net v2, NUnit) runs through `dotnet test`. This is decided per project, so a solution can mix both. The reason: on the .NET 10 SDK, `dotnet test` uses the VSTest target by default, which `xunit.v3` 4.x refuses to run under.

The two runners take different filter syntax, so each has its own property:

- `TestFilter`: a `dotnet test` expression such as `Category!=NoCI`, for the `dotnet test` projects.
- `TestingPlatformCommandLineArguments`: a platform command line passed through untouched, such as `--filter-not-trait Category=NoCI`, added to whatever the project sets in the MSBuild property of the same name.

Setting `TestFilter` while platform projects exist, without also setting `TestingPlatformCommandLineArguments`, fails the build and names the affected projects. Common translations:

| `TestFilter` | `TestingPlatformCommandLineArguments` |
|---|---|
| `Category!=NoCI` | `--filter-not-trait Category=NoCI` |
| `Category!=NoCI&Category!=Integration` | `--filter-not-trait Category=NoCI Category=Integration` |
| `Category=Unit\|Category=Integration` | `--filter-trait Category=Unit Category=Integration` |
| `Category=Unit&Speed!=Slow` | `--filter-trait Category=Unit --filter-not-trait Speed=Slow` |

## Customizing targets

Every target has a default implementation that a build can extend or replace. To add an ordering constraint while keeping the inherited dependencies:

```csharp
public Target Pack => d => d
    .Inherits<INuGetBuild>(b => b.Pack)
    .After<ILayeringBuild>(b => b.CheckLayering);
```

A component can also replace a target of a component it extends with an explicit implementation, which is how `IStandardBuild` narrows `IDefaultBuild.Test` to unit tests (`Target IDefaultBuild.Test => ...`).

## Generating CLI tool wrappers

The wrappers under `Infrastructure/Service/Tool/<Tool>/*.Generated.cs` (`aws`, `sam`, `dotnet lambda`, `pipenv`) are generated from `specification/*.json`, one type per file under a folder per tool, and treated as generated code (see the project's `.editorconfig`). After changing a specification, run:

```bash
make fallout TARGET=GenerateTools
```

See the [Fallout documentation](https://fallout.build/) for the specification format.
