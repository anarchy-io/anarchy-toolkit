# Automation Preferences

Automation preferences are optional solution-level overrides for the Anarchy Toolkit's build and scaffolding defaults.
They live in one hidden file at the solution root, `./.anarchy-toolkit.json`; no configuration file is needed to use
the CLI, Anarchy automation components or Make targets. `{}`, a partial file, or no file at all are valid. Omitted values use
explicit in-code defaults, not layout inference.

The JSON Schema is [`anarchy-toolkit.schema.json`](../schemas/anarchy-toolkit.schema.json), published at
`https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json`. All properties are optional at
every level. Configured relative paths are relative to the solution root. The file stores policy only: never put
credentials, API keys, CI identity, build numbers, application selections, release project/increment/head selections,
or destructive-operation approvals in it.

More generally, do not persist operation inputs or code hooks: application names to run or publish, Docker operation
container names or extra arguments, force/dry-run/confirmation switches, appsettings reset, automatic staging,
delegates, predicates, telemetry callbacks, or transient process/state paths. Preferences are data policy, not secrets,
capabilities or a record of one particular operation.

## Resolution and defaults

For Anarchy builds, the underlying Fallout engine's normal resolution wins first (command-line parameters, environment variables and
`parameters.json` as resolved by Fallout), followed by an override in `.anarchy-toolkit.json`, then the toolkit's
in-code default or derived value. Existing build-member overrides remain available. The standalone `anarchy-toolkit`
CLI uses explicit command arguments before saved overrides and then its in-code defaults. A saved preference is a
default, never a lock: a command can override it for one operation.

## Which settings file?

These locations have different owners and lifetimes; use the one that matches the value:

| Location | Owner | What belongs there | Required? | Precedence |
|---|---|---|---|---|
| Command-line arguments, environment variables, or Make variables forwarded to a build | The caller; Fallout resolves build parameters | One-run choices such as a target's `--LayeringMode`, build configuration, test filter, app name, or operation flags | No | Explicit operation/build inputs take precedence over saved preferences. Fallout resolves its arguments, environment and parameter file using its own rules. |
| `.fallout/parameters.json` | Fallout | The solution selector (`Solution`) and stable Fallout build parameters; keep engine-owned settings here | Needed by a Fallout build host to identify its solution; not needed by `anarchy-toolkit bootstrap` | Fallout resolves its values before the Toolkit reads saved preferences. |
| Root `.anarchy-toolkit.json` | Anarchy Toolkit | Reusable, non-secret policy overrides from the settings inventory below | No | Used only when no explicit argument or resolved Fallout parameter supplies the value; omitted settings use in-code defaults. |

Do not copy the same value into both JSON files just to make it effective. Put a one-time exception in operation arguments;
put a repeatable build-host setting in Fallout's parameter file; put a reusable Toolkit policy override in
`.anarchy-toolkit.json`. Neither file is a place for credentials or transient operation approvals.

Layering follows the same explicit-override → saved-override → default model. With nothing saved or passed, the
effective baseline is folder mode under `src`: standard layers (`Core`, `Infrastructure`, `Bootstrap`, `Runtime`,
`Apps`), libraries under `src/Library`, and standard application folders `src/Apps/Apis`, `src/Apps/Jobs` and
`src/Apps/Ui` (all in the `Apps` layer). The effective layout is validated before use. The tool does not guess a mode from
project names or folders. Select prefix mode explicitly when needed; folder-only defaults are suppressed for prefix
mode. A single-application bootstrap can use `--layout single-app` (or explicitly set `libraryRoot: "src"` and one
application-folder mapping) without changing the global baseline.

Fallout parameters therefore remain the best way to make a one-command exception, while the root file is for a
deliberate reusable preference. Normal build execution never creates or updates this file. Preferences have one
solution-root location and no alternate-directory setting.

`--verbosity Quiet` also quiets the build's informational target output and defaults child-tool verbosity to quiet.
An explicit `--tool-verbosity` still takes precedence. Tests that invoke automation tasks directly pass quiet verbosity
so successful helper operations do not bury the test summary; errors remain failures rather than being silenced.

## Settings inventory

Use camelCase JSON names. The schema describes types, constraints and defaults; this table summarizes the supported
policy. A missing property is not an error. Some settings deliberately have no global default and are required only
by operations that need them.

| Section | Properties and purpose |
|---|---|
| `layering` | `mode`, `layers`, checked `root`, `libraryRoot`, and `applicationFolders`. Optional; effective omissions use the folder baseline above. Prefix mode does not inherit folder mappings. |
| `build` | `configuration`, production `sourceRoot`, `testsRoot`, `toolVerbosity`, `treatWarningsAsErrors`. `sourceRoot` is distinct from layering's checked root. |
| `tests` | `commonProject` (the shared, non-executable test project every test project references; default `Common`, i.e. `tests/Common`), `kindAliases`, VSTest `filter`, and Microsoft.Testing.Platform `testingPlatformArguments`. The two runner filter syntaxes remain separate. |
| `docker` | `composeFile`, `projectName`, startup `waitFor` list, `waitForHealthy`, readiness `timeout`, and `startTimeout`. |
| `mockServer` | `project`, `port`, `expectations`, `record`, `verbosity`, `ui`, extra `args`, `timeout`, `openApi`, and `openApiOutput`. |
| `documentation` | `dependencyMapOutput`, DocFX `apiReferenceConfig`, and `apiReferenceMetadataOutput`; derived output paths follow the configured parent paths. |
| `dependencies` | `versionLock` policy only. This does not grant permission to install a tool or upgrade packages. |
| `nuget` | Publishing `source` and `skipDuplicate`; never feed credentials or API keys. A feed is needed only for publishing. |
| `release` | Comparison `base` only. Project, increment and head are operation-specific. |
| `container` | `registry`, optional `imageName`, `dockerfile`, `runtimeIdentifier`. Image names and runtime remain derived when absent; tags are invocation-specific. A registry is needed only for image operations. |
| `agentSkills` | `prefix` and ordered, solution-relative `directories`; the first directory is the creation destination. Absolute paths and traversal are rejected for installation and synchronization. |
| `gitHooks` | `skipLintFix`, `skipTestStatic`, and `skipTestRuntime`; hook choice, force and dry-run remain operation-specific. |
| `boilerplate` | Default `items` and `exclude` selection; an explicit install selection wins. |
| `scaffolding` | `swagger` (whether generated APIs include Swagger through `Anarchy.Toolkit.Runtime.Web.Swagger`; default `true`), `swaggerTheme` (the default Swagger UI theme choice; default `true`) and `appSettings` (`isolated`, the default: each application has its own settings; `shared`: applications use one `AppSettings` in the Infrastructure library and one `ModuleAppSettings` with `appsettings*.json` in the Bootstrap library). |

Collections replace rather than append. Explicit `false`, empty collections and supported empty strings can intentionally
override saved values. Do not copy the schema's defaults into the file just to make them effective.

## Inspect and edit preferences

The global tool and Anarchy build components use the same validated preference editor. `show` reports each effective
preference with its source (`saved value` or `in-code default`), separately from the saved document and schema fallbacks,
and does not create a file. It cannot show arguments for a different build invocation. The Anarchy build's
`ShowAnarchyPreferences` target labels active layering values supplied as Fallout parameters when they are passed to
that target. `set` accepts a JSON value, and `unset` removes that override so the next-precedence source applies:

```bash
anarchy-toolkit config show
anarchy-toolkit config set build.configuration '"Release"'
anarchy-toolkit config set docker.waitFor '["postgres", "localstack"]'
anarchy-toolkit config unset build.configuration
anarchy-toolkit config configure
```

The distributed Makefile wraps the corresponding Anarchy build targets:

```bash
make config_show
make config_set KEY=build.configuration VALUE='"Release"'
make config_unset KEY=build.configuration
make config_configure
```

Quote JSON values so the complete value reaches the command as one argument; strings require JSON double quotes inside
the shell quoting, while booleans, arrays and objects are JSON literals.

Edits are always explicit. `set` merges only the selected property and preserves unrelated preferences. It rejects
unknown keys, secrets and invocation-only settings; validates the resulting document and effective combinations
before writing; and writes atomically. An invalid existing file is reported and left untouched. `configure` previews
the proposed changes and asks before saving. Neither editing route runs builds, installs dependencies, publishes,
stages files or grants destructive-operation permissions.

## Saving during bootstrap or scaffolding

Bootstrap and scaffold do not save preferences automatically. Use `--save-preferences` to request persistence.
Interactive wizards offer to save the selected, persistable preferences and default the answer to **No**. Noninteractive
commands save only when the explicit flag is passed. Only preferences deliberately selected for that operation are
saved; transient identifiers and the rest of the effective configuration are not copied. Existing unrelated settings
are preserved.

Without saving, generation still uses the selected layout for that invocation and reports any nondefault layout
arguments that later commands need. An adopter can repeat those arguments or provide them from a custom skill; a
preferences file is a reproducibility convenience, not a prerequisite.
