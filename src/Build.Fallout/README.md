# Anarchy.Toolkit.Build.Fallout

Reusable [Fallout](https://fallout.build/) build components for .NET repositories.

```bash
dotnet add build/_build.csproj package Anarchy.Toolkit.Build.Fallout
```

Implement the components appropriate to your repository on a `FalloutBuild` class. For example, a package-only build can implement `INuGetBuild`, `ILayeringBuild`, `IDependencyMapBuild`, `IDependencyUpgradeBuild` and `IGitHooksBuild`, with an empty `Artifacts` collection. Use `IBoilerplateBuild` for scaffolding, or add the AWS, container, CodeDeploy and Slack components as needed. External deployment and publication targets require their respective tools and credentials.

```csharp
using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.Common;

class Build : FalloutBuild, INuGetBuild, ILayeringBuild, IDependencyMapBuild,
              IDependencyUpgradeBuild, IGitHooksBuild
{
    public IReadOnlyCollection<Artifact> Artifacts => [];
    public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
}
```

Create `.fallout/parameters.json` with the solution path, then run `dotnet run --project build/_build.csproj -- --help` to see targets and parameters. The included scaffolding target writes its solution setting to `.fallout/parameters.json`; it will not overwrite existing files.

See the [Fallout builds guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/fallout-builds.md) for component details and package publishing.

## Starting from an empty folder

With the .NET 10 SDK, Bash and Make installed, create a build host first:

```bash
mkdir -p build .fallout
dotnet new console --framework net10.0 --name _build --output build
dotnet add build/_build.csproj package Anarchy.Toolkit.Build.Fallout
printf '{}\n' > .fallout/parameters.json
```

Use the example above in `build/Program.cs`, adding `IBoilerplateBuild` to the class. Then generate a starter solution and try the build:

```bash
dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-solution-name MyApp
make compile FALLOUT_RELOAD_ENV=false
make check_layering FALLOUT_RELOAD_ENV=false
```

By default, scaffolding creates a solution containing the build and a starter `src/Core` project, along with `tests/`, `samples/`, `docs/`, `Makefile`, scripts and configuration files. Review the supplied .NET SDK and test-runner settings and enable or adapt the optional Make targets for your build components and tools.

### Choosing what to install

Use `--boilerplate-items` for a comma-separated allowlist and `--boilerplate-exclude` to remove items. Names are case-insensitive; omit the allowlist to select everything.

| Item | Effect |
|---|---|
| `solution` | Create the named `.sln`, add the existing build and selected Core project, and set `Solution` in `.fallout/parameters.json`. Requires `--boilerplate-solution-name`. |
| `core` | Create `src/Core/Core.csproj`. Add it to a solution yourself if `solution` is excluded. |
| `tests`, `samples`, `docs` | Create the directory and its `.gitkeep`. |
| `Makefile`, `.gitignore`, `.editorconfig`, `global.json`, `nuget.config` | Install the corresponding packaged file. |
| `Directory.Build.props` | Create the SQL resource embedding rule if the file is absent. |
| `scripts` | Install all scripts; individual paths such as `scripts/build.fallout.sh` or `scripts/bootstrap` can also be selected or excluded. |

`all` selects every item and `none` selects nothing. Unknown names fail before any files are written. When selecting individual scripts, include their helpers: the launcher needs `scripts/shared/colors.env`, `scripts/console/subtitle.sh` and, unless environment reloading is disabled, `scripts/bootstrap/shell-profile-path.sh`.

Existing `.gitignore` and `.editorconfig` files are untouched unless `--append-existing` (or `--apend-existing`) is supplied; packaged content is then appended at most once. Review the result because later rules can change earlier ones. Existing `global.json`, `nuget.config` and `Directory.Build.props` are always preserved. The target refuses to overwrite a solution, selected script, Makefile, starter project, `.gitkeep` or existing `Solution` parameter. If an operation fails partway through, inspect the output before retrying.

## Releases

### 1.0.2

- chore(deps): update AWS SDK packages
