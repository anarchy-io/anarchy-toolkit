# NUKE Builds

`Anarchy.Toolkit.Build.Nuke` (`src/Build.Nuke`) holds the toolkit's [NUKE](https://nuke.build) build components. They are packaged as build components so every repository gets the same targets. The toolkit's own `./build` is its first consumer. 🔧

## Using it

A build project is a `partial` class that implements the components it needs; anything specific to the repository goes in its own `Build.*.cs` files:

```csharp
using Anarchy.Toolkit.Build.Nuke.Component;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Tools.DotNet;

namespace Build;

[ShutdownDotNetAfterServerBuild]
[DotNetVerbosityMapping]
[TelemetryOptout] // mandatory: without it NUKE sends telemetry, which may trigger security alerts
internal sealed partial class Build : NukeBuild, IStandardBuild, IFullReleaseBuild, IContainerBuild
{
    public IReadOnlyCollection<Artifact> Artifacts => [ /* see "Artifacts" */ ];

    public string ContainerRegistry => "123456789012.dkr.ecr.eu-west-1.amazonaws.com";

    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
}
```

Reference the package (`<PackageReference Include="Anarchy.Toolkit.Build.Nuke" />`) from `build/_build.csproj`. The package carries a `buildTransitive` targets file that serves the `NuGet.*` assemblies from the installed SDK, so the build keeps working when an agent moves to a newer SDK feature band (otherwise: *Could not load file or assembly 'NuGet.Frameworks, Version=7.9.0.0'*). A `ProjectReference`, as the toolkit's own build uses, does not import it automatically: import `buildTransitive/Anarchy.Toolkit.Build.Nuke.targets` from `build/Directory.Build.targets`.

## Components

| Component | Targets | Notes |
|---|---|---|
| `IDefaultBuild` | `Clean`, `Build`, `Test`, `PullRequest`, `SetupLocal`, `PrintArtifactsJson` | The base of every component: solution and directory conventions, `Artifacts`, `TestConfiguration`, `TestFilter`/`TestingPlatformCommandLineArguments`, user secrets. |
| `IVersionedBuild` | — | Adds the `--build-number` parameter. |
| `IStandardBuild` | `Restore`, `Compile`, `TestUnit`, `TestIntegration`, `TestEndToEnd`, `TestAll`, `Lint`, `LintFix`, `ListProjects`, `RunApp`, `RunApps`, `GenerateAppSettings` | Overrides `Test` (unit tests only, so releases never need test infrastructure), `PullRequest` (`Lint` + `TestAll`), `TestsDirectory` (`tests`) and `IsTestProject` (ignores test kits under `src`). |
| `INuGetBuild` | `ListPackages`, `GenerateLocalRelease`, `ValidateLocalRelease`, `Pack`, `Publish` | Versions source projects, and packs and publishes the batch in package dependency order. |
| `IContainerBuild` | `EcrPublishAppArtifact`, `EcrImageBuild`, `EcrImagePush` | ECR flow for `--app-name`. Implement `ContainerRegistry`; image name, tag (short commit SHA), Dockerfile (`docker/ecs/Dockerfile`) and runtime have defaults. |
| `ICodeDeployBuild` | `BuildReleaseCodeDeploy`, `PublishLinux`, `PackCodeDeploy`, `ValidateDeploymentAcceptance`, `DeployArtifacts` | Acts on artifacts with a `CodeDeploy.ArchiveName`. |
| `IAwsLambdaBuild` | `BuildRelease`, `PublishLambda`, `PublishLambdaCI`, `AwsCliPackage`, `DeployLocal`, `Run` | Acts on artifacts with an `AwsLambda.ArchiveName`. `UseV2Deployment` switches to `dotnet lambda package-ci`. |
| `IFullReleaseBuild` | `BuildReleaseFull` | Lambdas and CodeDeploy in one target. |
| `IHasEndToEndTests` | `EndToEndTest` | Projects matching `*.Tests.EndToEnd`; triggered by `ValidateDeploymentAcceptance`. |
| `IReportFailureOnSlack` | `ReportFailure` | Implement `ApplicationName` and `EncryptedSlackWebhookUrls` (KMS-encrypted per environment). The `[ReportFailureOnSlack]` attribute is obsolete and no longer needed. |
| `IDependencyMapBuild` | `GenerateDependencyMap` | Writes a static HTML page (`--dependency-map-output`, default `docs/dependency-map.html`): a Mermaid graph of project references with layering violations highlighted, an onion of the layers (click a ring, or a group outside the chain, to zoom in on the references between its own projects; `#<Group>` in the URL opens it directly) and a table of projects, files and lines per group. Override `DependencyMapLayers` and `GetDependencyMapGroup` for other naming conventions. It carries no timestamp, so CI can regenerate it and commit only when it changed. |
| `ILayeringBuild` | `CheckLayering` | Fails when the layering is broken: a project reference, a source file `using` or a root layer folder pointing to a higher layer, a production project referencing a testing kit, or a project under `src/` without a known layer. By default layers come from project-name prefixes; `--layering-by-folder true` uses the first folder under `src/` for every project instead, with no fallback (see the [architecture guide](architecture.md#enforcing-the-rule)). Override `LayeringLayers` and `GetLayer` (return `null` to leave a project out) for other conventions. |

Run `./scripts/build.nuke.sh --help` for the full list of targets and parameters, or `--plan` for the execution plan.

### NuGet packages

Implement `INuGetBuild` to pack projects under `src/`, including projects not yet in the solution (override `NuGetProjects` to select others). `Pack` compiles the solution and writes packages and symbols into a fresh `artifacts/packages/<run-id>/` directory; each project's MSBuild packaging settings determine its metadata, version, packability and symbol format. `Publish` runs `Pack` first, validates the whole batch and pushes packages sequentially, with every dependency in the batch before its consumers, including dependencies within the same layer and across target frameworks. Cycles, duplicate package IDs and incompatible in-batch dependency versions stop publication before the first push. Dependencies outside the batch must already exist on the feed. A failed push stops the rest; already pushed packages cannot be rolled back.

For example, `./scripts/build.nuke.sh Publish --nu-get-source https://api.nuget.org/v3/index.json` uses configured credentials, or supply `--nu-get-api-key` (prefer a secret environment variable in CI). Duplicate versions are skipped by default; set `--skip-duplicate false` to fail instead. `dotnet nuget push` handles matching `.snupkg` files alongside packages when the feed supports symbols. A successful push does not guarantee immediate availability on feeds with indexing delays.

To prepare a release locally, run `./scripts/build.nuke.sh GenerateLocalRelease --release-project Core.Events --release-bump minor` (also `major` or `patch`). It increments the selected project's own version and every transitive source-project dependent by the same kind of increment, once each, including test kits. It uses project references, not layer names, and does not commit or publish. Projects must have one explicit unconditional stable `major.minor.patch` version; prereleases and computed versions are not supported. Review and commit the resulting project files with the changes.

`./scripts/build.nuke.sh ValidateLocalRelease --release-base origin/main` compares current project versions with a Git commit or ref. For every existing package whose version increased, all existing transitive dependents must have at least the same severity bump (major > minor > patch); decreases fail. New projects have no baseline and are excluded from this comparison. The PR workflow compares against the PR base commit with full Git history. This checks propagation of version changes, **not** whether source changes themselves require a version bump. Merges and other pushes to `main` run the publish workflow, which also supports manual retries. Every run attempts every package, so all versions not yet on NuGet can be published, including on the first run.

### How `IStandardBuild` finds test projects

`TestUnit`, `TestIntegration` and `TestEndToEnd` run the test projects of their kind. A project is of a kind when its name *is* the kind (a single `Unit` project) or when one of its dot-separated segments is the kind and another is `Tests`: `Tests.Unit.Core.Primitives`, `MyService.Unit.Tests` and `MyService.Tests.Unit` are all unit test projects. Override `IsTestProjectOfKind` to change that. When `Compile` ran first, the tests skip restoring and building.

### Local `appsettings.Development.json`

`GenerateAppSettings` (also triggered by `SetupLocal`; `make appsettings` in this repo) writes the untracked `appsettings.Development.json` next to every tracked `appsettings.json.template` (override `AppSettingsTemplates` to choose which). The template holds only the keys a developer must override from the shared `appsettings.json`, and each value is a short explanation of what is expected and, optionally, where to find it:

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
    new() {
        ProjectFile = this.As<IDefaultBuild>().SourceDirectory / "Api" / "Orders" / "Orders.csproj",
        AwsLambda = { ArchiveName = this.As<IDefaultBuild>().ArtifactsDirectory / "Lambda.Shop.Api.Orders.zip" },
    },
    new() {
        ProjectFile = this.As<IDefaultBuild>().SourceDirectory / "Jobs" / "Sync" / "Sync.csproj",
        CodeDeploy = { ArchiveName = "shop.sync" },
    },
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

## Test settings

On build servers, `SetTestEnvironmentVariables` (and `SetEndToEndTestEnvironmentVariables`) export `TestConfiguration` (and `EndToEndTestConfiguration`) as environment variables before the tests run. Plain strings are used as-is; values wrapped in `Secret` are decrypted with KMS (`--aws-profile`, `--aws-region`). `DecryptSecret` is also available as an extension method.

```csharp
public IReadOnlyDictionary<string, ConfigurationValue> TestConfiguration => new Dictionary<string, ConfigurationValue> {
    { "HELLO", "WORLD" },
    { "SUPERSECRET", new Secret("AQICAH...") },
};
```

## Customizing targets

Every target has a default implementation that a build can extend or replace. To add an ordering constraint while keeping the inherited dependencies:

```csharp
public Target PublishLinux => d => d
    .Inherits<ICodeDeployBuild>(b => b.PublishLinux)
    .After(Build);
```

A component can also replace a target of a component it extends with an explicit implementation, which is how `IStandardBuild` narrows `IDefaultBuild.Test` to unit tests (`Target IDefaultBuild.Test => ...`).

## Generating CLI tool wrappers

The wrappers under `Infrastructure/Service/Tool/*.Generated.cs` (`aws`, `sam`, `dotnet lambda`, `dotnet user-secrets`, `pipenv`) are generated from `specification/*.json` and treated as generated code (see the project's `.editorconfig`). After changing a specification, run:

```bash
./scripts/build.nuke.sh GenerateTools
```

See [NUKE: CLI tools](https://nuke.build/docs/common/cli-tools/) for the specification format.
