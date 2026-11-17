# Adopting Anarchy Toolkit: instructions for coding agents

You are a coding agent asked to adopt [Anarchy Toolkit](https://github.com/anarchy-io/anarchy-toolkit) in the current
directory, or to migrate a repository that already uses it to the latest version. These instructions are for you, not
for humans: follow them in order, run the commands yourself and keep the user informed. The human documentation is at
https://anarchy-io.github.io/anarchy-toolkit/.

## Ground rules

- Never overwrite, delete or revert files you did not create; the toolkit's generators refuse to overwrite anything,
  so treat a conflict as a question for the user, not something to force.
- Ask the user before: choosing a path when the situation is ambiguous, renaming or moving projects, publishing,
  pushing, or anything that destroys data.
- Prefer the toolkit's commands (`anarchy-toolkit`, `make`, Fallout targets) to hand-written equivalents.
- Work on a branch and keep the user's uncommitted changes intact (`git status` first).
- Report what you did, what you skipped and why, and the commands the user should know about.

## 1. Check the prerequisites

Run and report each:

```bash
dotnet --list-sdks   # needs a 10.0 SDK
make --version       # GNU Make, for the Makefile (macOS, Linux or WSL)
bash --version
git status --short
```

If the .NET 10 SDK is missing, ask the user to install it from https://dotnet.microsoft.com/download (or, once the
boilerplate is installed, run `make install_sdk`). Install or update the CLI tool:

```bash
dotnet tool update --global Anarchy.Toolkit.Build.Cli.Tool   # installs it when missing
anarchy-toolkit --help
```

## 2. Work out the situation

Inspect the directory and pick exactly one path. Confirm it with the user if it is not obvious.

| What you find | Path |
|---|---|
| Empty directory (or only a README, licence or `.git`) | [A. New solution](#a-new-solution) |
| A .NET repository without `build/_build.csproj` | [B. Existing repository](#b-existing-repository) |
| An application that only needs a capability (persistence, messaging, observability, web plumbing) | [C. Individual packages](#c-individual-packages) |
| `build/_build.csproj` referencing `Anarchy.Toolkit.Build.Fallout`, or `Anarchy.Toolkit.*` package references | [D. Migrate to the latest version](#d-migrate-to-the-latest-version) |

## A. New solution

Ask the user for the solution name and which applications to start with (APIs, jobs, UIs), then run the tool without
its interactive wizard:

```bash
anarchy-toolkit bootstrap <SolutionName> --output . --with-skills
anarchy-toolkit scaffold api <Name>   # per API; also: scaffold job <Name>, scaffold ui <Name>
make install
```

`--with-skills` installs the agent skills (see [step 3](#3-install-the-agent-skills)); drop it if the user declines.
Default layering is by folder with the layers `Core, Infrastructure, Configuration, Hosting, Web`; if the user wants
another layering, ask them to run `anarchy-toolkit bootstrap` interactively instead.

## B. Existing repository

1. Add the Fallout build host (replace `MyApp.sln` with the repository's solution file):

   ```bash
   mkdir -p build .fallout
   dotnet new console --framework net10.0 --name _build --output build
   dotnet add build/_build.csproj package Anarchy.Toolkit.Build.Fallout
   printf '{ "Solution": "MyApp.sln" }\n' > .fallout/parameters.json
   ```

   If `.fallout/parameters.json` exists, add the `Solution` property to it instead. If the repository manages package
   versions centrally (`Directory.Packages.props`), add the `PackageVersion` there.

2. Replace `build/Program.cs` with a build class. Keep the components that fit the repository (see
   https://anarchy-io.github.io/anarchy-toolkit/getting-started/fallout/); `IContainerBuild` needs a
   `ContainerRegistry`, so only add it when the user wants container images.

   ```csharp
   using Anarchy.Toolkit.Build.Fallout.Component;
   using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
   using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
   using Fallout.Common;

   class Build : FalloutBuild, IStandardBuild, ILayeringBuild, IDependencyMapBuild, INuGetBuild,
                 IDependencyUpgradeBuild, IGitHooksBuild, IBoilerplateBuild, IDockerBuild, IAgentSkillsBuild
   {
       public IReadOnlyCollection<Artifact> Artifacts => [];
       public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
   }
   ```

3. Install the Makefile and scripts, appending the toolkit's Git and editor settings to existing ones:

   ```bash
   dotnet run --project build/_build.csproj -- SetupBoilerplate --boilerplate-items Makefile,scripts,.gitignore,.editorconfig --append-existing
   ```

   It fails without writing anything when a file would be overwritten: report the conflicts to the user. Add
   `global.json` or `nuget.config` to the items only when the repository has none.

4. Add the solution to the build (`dotnet sln add build/_build.csproj`), then verify:

   ```bash
   make compile
   make test_unit
   make check_layering
   ```

   `check_layering` will likely report violations in a repository not organised by layers. Do not restructure the
   code on your own: show the user the violations, explain the layers (`Core < Infrastructure < Configuration <
   Hosting < Web`, see https://anarchy-io.github.io/anarchy-toolkit/guides/architecture/) and the options
   (`--layering-by-folder`, `LayeringLayers` in `.fallout/parameters.json`), and let them decide.

## C. Individual packages

Pick packages from the catalog at https://anarchy-io.github.io/anarchy-toolkit/packages/ with the user, then for each:

```bash
dotnet add <Project>.csproj package Anarchy.Toolkit.<Package>
```

Read the package README (`https://raw.githubusercontent.com/anarchy-io/anarchy-toolkit/main/src/<Package>/README.md`)
and wire its module as it describes. Build and run the tests afterwards. Offer path B if the user also wants the build.

## D. Migrate to the latest version

1. Update the tool and see what is outdated:

   ```bash
   dotnet tool update --global Anarchy.Toolkit.Build.Cli.Tool
   make upgrade_dependencies INSTALL_TOOL=true
   ```

2. Read the `## Releases` section of each used package's README
   (`https://raw.githubusercontent.com/anarchy-io/anarchy-toolkit/main/src/<Package>/README.md`) between the current
   and the latest version, and list the breaking changes (major versions, `!` in a title) for the user.
3. With the user's agreement, upgrade (`make upgrade_dependencies UPGRADE=true`) and apply the changes the release
   notes call for.
4. Refresh the boilerplate: newer Makefile and scripts are not installed over existing ones. Compare with the packaged
   files (`~/.nuget/packages/anarchy.toolkit.build.cli/<version>/boilerplate/`) and propose the differences to the
   user rather than replacing their files.
5. Verify with `make compile`, `make test`, `make lint` and `make check_layering`.

## 3. Install the agent skills

Ask the user whether to install the toolkit's agent skills in `.agents/skills`: `anarchy` (the entry point) and one
skill per area (develop, environment, release, maintenance), so later agent sessions run the solution's operations
correctly. They are never installed by default. If the user agrees and they are not installed yet:

```bash
anarchy-toolkit skills install
make skills_validate   # when the build implements IAgentSkillsBuild
```

If the repository has its own make targets, offer to mark them with `#@ <skill>` (see the `anarchy-maintenance` skill),
run `make skills_sync`, then replace the generated skills' `TODO`s with how and when to use those targets:

```bash
make skills_sync
make skills_validate
```

Then load the `anarchy` skill for the rest of the session.

## 4. Finish

- Run `make help` and show the user the main targets.
- Summarise the changes, the commands run and their results, and anything left for the user to decide.
- Suggest committing on a branch; do not commit or push unless the user asks.
