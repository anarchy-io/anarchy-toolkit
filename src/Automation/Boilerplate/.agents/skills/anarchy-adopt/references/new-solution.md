# New solution

Use this recipe for an empty directory or one containing only a README, license or `.git` directory. Ask for the
solution name and the APIs, jobs or UIs to create; do not launch the interactive wizard from a non-interactive session.

## Prerequisites

Check `dotnet --list-sdks` for .NET 10, and `make --version` / `bash --version` if using the generated Makefile. Install
or update the CLI with `dotnet tool update --global Anarchy.Toolkit.Automation.Build.Cli`, then check
`anarchy-toolkit --help`.

## Create and verify

```bash
anarchy-toolkit bootstrap <SolutionName> --output . --with-skills
anarchy-toolkit scaffold api <Name>   # repeat as needed; also: scaffold job <Name>, scaffold ui <Name>
anarchy-toolkit scaffold library Orders --layer Core
anarchy-toolkit scaffold tests unit
make install
make pull_request
```

Drop `--with-skills` if the developer declines installing skills. Bootstrap uses folder layering by default under
`src/Library`, with applications under `src/Api`, `src/Jobs` or `src/Ui`; `--layering prefix` chooses name-prefix
layering. The operation's layout is not saved unless `--save-preferences` is passed or the developer accepts the
interactive save prompt (default No). Later operations can take explicit layout arguments or use saved preferences.

Before scaffolding later applications, read the root `.anarchy-toolkit.json` if present and honor its layering mode,
layers, root and folder mappings. Folder mode supports `libraryRoot` and `applicationFolders`. Missing config or mode
is valid; never infer the mode. See the [preferences guide](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-preferences.html).

After `scaffold`, review the generated application's settings (auth URLs and client IDs), then run `make appsettings`
for local settings. Don't commit `appsettings.Development.json`.

Use `scaffold library <Name> --layer <Layer>` for layer projects and `scaffold tests [unit|integration|endtoend]` for
the shared test project per kind; test scaffolding honors `TestKindAliases`. Both accept `--dry-run`; selected layout
preferences are only saved with `--save-preferences`.

To opt out of layering, remove `ILayeringBuild` from `build/Build.cs`; to keep the check with a different layout, adapt
it through preferences. For direct build invocation on Windows, see [Without Make](https://anarchy-io.github.io/anarchy-toolkit/guides/automation-builds.html#without-make-including-windows).
