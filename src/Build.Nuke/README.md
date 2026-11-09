# Anarchy.Toolkit.Build.Nuke

Reusable [NUKE](https://nuke.build) build components for .NET repositories.

```bash
dotnet add package Anarchy.Toolkit.Build.Nuke
```

- Implement components such as `IStandardBuild` and `INuGetBuild` on your NUKE build class for standard build, test, pack and publish targets.
- Optional components cover containers, AWS Lambda, CodeDeploy, dependency maps, layering checks and Slack failure reporting.
- Test targets support Microsoft.Testing.Platform as well as VSTest. The package includes a `buildTransitive` targets file for SDK-provided NuGet assemblies.

## When to use it

Use this package in a repository's NUKE build project when you want a common local and CI build without copying target implementations. Select only the components that fit the repository: a package-only project can use `INuGetBuild`, while applications can add container or deployment components. The package is build tooling, not an application runtime dependency.

## Using an existing repository

1. Reference the package from the NUKE build project and implement the required component interfaces on the build class. Supply repository-specific properties such as `Artifacts`; a package-only build can use an empty collection.
2. Run targets through your NUKE host, for example `dotnet run --project build/_build.csproj -- Compile` and `dotnet run --project build/_build.csproj -- TestUnit`. Check `--help` or `--plan` for the targets and parameters available in your build.
3. Use `ILayeringBuild` or `IDependencyMapBuild` if the repository follows the supported layer conventions. Override their configuration when your naming or directory layout differs.
4. For NuGet publication, review project versions and package metadata before running `Pack` or `Publish`. Publishing is a real external operation; configure feed credentials securely and verify the target feed first.

Deployment components require their corresponding external tooling, credentials and artifact configuration. See the [component reference](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/nuke-builds.md#components) for the available targets and prerequisites.

## Starting from an empty folder

With the .NET 10 SDK, Bash and Make installed, create the small NUKE host first (no solution is needed yet). Use a package version containing `IBoilerplateBuild`:

```bash
mkdir -p build .nuke
dotnet new console --framework net10.0 --name _build --output build
dotnet add build/_build.csproj package Anarchy.Toolkit.Build.Nuke
printf '{}\n' > .nuke/parameters.json
```

Replace `build/Program.cs` with:

```csharp
using Anarchy.Toolkit.Build.Nuke.Component;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension;
using Nuke.Common;

[TelemetryOptout]
class Build : NukeBuild, IBoilerplateBuild, ILayeringBuild, INuGetBuild, IDependencyMapBuild
{
    public IReadOnlyCollection<Artifact> Artifacts => [];
    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
}
```

Then generate the scaffold and try the build:

```bash
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-solution-name MyApp
make compile NUKE_RELOAD_ENV=false
make check_layering NUKE_RELOAD_ENV=false
```

By default, `SetupBoilerplate` creates `MyApp.sln` containing the build and a starter `src/Core` project, along with `tests/`, `samples/`, `docs/`, `Makefile`, `scripts/`, `.gitignore`, `.editorconfig`, `global.json`, `nuget.config` and `Directory.Build.props`. The last file contains only the MSBuild rule that embeds `**/Queries/**/*.sql`. The packaged `global.json` selects .NET 10 and the Microsoft.Testing.Platform test runner; review it for your projects. Add projects named after their layer (for example `Infrastructure.Storage` or `Web.Api`) and test projects under `tests/` as needed. The supplied Makefile includes optional packaging, dependency-map and Git-hook commands; enable or adapt the corresponding build components and tools before using them.

### Choosing what to install

Use `--boilerplate-items` for a comma-separated allowlist, and `--boilerplate-exclude` to remove items from it. Both are case-insensitive; omit the allowlist to select everything. The available names are:

| Item | Effect |
|---|---|
| `solution` | Create the named `.sln`, add the existing `build/_build.csproj` and the new Core project if selected, and set `Solution` in `.nuke/parameters.json`. Requires `--boilerplate-solution-name`. |
| `core` | Create `src/Core/Core.csproj`. If `solution` is not selected, add it to a solution yourself. |
| `tests`, `samples`, `docs` | Create the respective directory and its `.gitkeep`. |
| `Makefile`, `.gitignore`, `.editorconfig`, `global.json`, `nuget.config` | Install the corresponding packaged file. |
| `Directory.Build.props` | Create the SQL resource rule if the file is absent. |
| `scripts` | Install all packaged scripts. A specific script or subtree such as `scripts/build.nuke.sh` or `scripts/utils/git-hooks` can be selected or excluded instead. |

`all` selects every item; `none` selects nothing. Unknown names fail before any files are written. For example:

```bash
# Only install selected settings in an existing repository; no solution name is required.
dotnet run --project build/_build.csproj -- SetupBoilerplate \
  --boilerplate-items .gitignore,.editorconfig,Directory.Build.props --apend-existing

# Create a new solution without an SDK pin, NuGet configuration or Git hooks.
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-solution-name MyApp \
  --boilerplate-exclude global.json,nuget.config,scripts/utils/git-hooks
```

Existing `.gitignore` and `.editorconfig` files are left untouched unless `--apend-existing` (also spelled `--append-existing`) is supplied. Then the packaged contents are appended literally, at most once; existing bytes are preserved. Review the result: later Git ignore rules and EditorConfig sections can change the effect of earlier ones, and an appended EditorConfig `root` directive does not replace the original top-level directive. Existing `global.json`, `nuget.config` (including case variants such as `NuGet.Config`) and `Directory.Build.props` are always left untouched, even with the append option. To add the SQL rule to an existing props file, edit it yourself.

The target never overwrites an existing solution, selected script, Makefile, starter project or `.gitkeep`; it fails before writing when one conflicts. It also refuses to replace an existing `Solution` parameter. Exclude items you do not want it to handle; with `solution` excluded it does not inspect or change the existing solution or NUKE parameters. If a command fails partway through, inspect the output before retrying.

To check a different source root, use `./scripts/build.nuke.sh CheckLayering --layering-root components` or `make check_layering LAYERING_ROOT=components`. The path is relative to the solution directory (absolute paths also work). Add `--layering-by-folder true` or `BY_FOLDER=true` to derive layers from the first folder under that root instead of project names.

See the [NUKE builds guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/nuke-builds.md) for setup, component details and package publishing, and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages
- build: improve development environment setup
- feat(release): automate package versions and release notes

### 1.1.0

- Added configurable solution scaffolding, packaged repository tooling and selectable layering roots.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced reusable NUKE build components, generated tool wrappers and transitive build targets.
