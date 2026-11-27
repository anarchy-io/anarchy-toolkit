# New Solution with the CLI

The `anarchy-toolkit` .NET global tool is the entry point to the platform's **Build Ecosystem**: it creates solutions and adds applications to them. It is the quickest way to adopt all three pillars at once, with a layered solution (Architecture), applications wired to the toolkit packages (Infrastructure) and the Anarchy build already in place, powered by [Fallout Build](https://fallout.build/).

## Install

The tool is published to NuGet.org as [`Anarchy.Toolkit.Automation.Build.Cli`](https://www.nuget.org/packages/Anarchy.Toolkit.Automation.Build.Cli).

```bash
dotnet tool install --global Anarchy.Toolkit.Automation.Build.Cli
anarchy-toolkit --help
```

Update it later with `dotnet tool update --global Anarchy.Toolkit.Automation.Build.Cli`.

## Bootstrap a solution

Run the wizard and answer its questions:

```bash
anarchy-toolkit bootstrap
```

It asks for:

1. the solution name and output directory, which must be empty or not exist yet;
2. whether it will contain one application or several, before choosing a layering mode;
3. for one application, its kind and folder name (default: `App`); for several, which applications to start with and a name for each;
4. how projects are mapped to layers: by folder (the default) or by name prefix;
5. the layers, innermost first (default: `Core, Infrastructure, Bootstrap, Runtime, Apps`); a library project is created per layer but `Apps`, each referencing the one below it, and the applications reference the Runtime library;
6. for each API, whether to include Swagger and its theme, and the optional entry points: a container for APIs and UIs, an AWS Lambda for APIs and jobs;
7. in a monorepo, whether the applications share their settings (one `AppSettings` in the Infrastructure library, `ModuleAppSettings` and `appsettings*.json` in the Bootstrap library) or each has its own;
8. whether to create the `Unit`, `Integration` and `EndToEnd` test projects (default: yes, with a shared `tests/Common` project) and a mock server host in `tests/MockServer` (default: no);
9. whether to install the agent skills (default: no; see [Agent skills](#agent-skills));
10. whether to save selected automation preferences (default: no).

It then shows the plan, including each web application's local ports, and writes nothing until you confirm. To skip the questions and create the baseline solution only:

```bash
anarchy-toolkit bootstrap MyApp --output ./MyApp [--with-skills] [--no-tests] [--with-mock-server] [--isolated-app-settings] [--layout monorepo|single-app] [--application-folder MyApp] [layout options] [--save-preferences]
```

`--layout monorepo` keeps shared libraries under `src/`, grouped in `src/Library`, with applications in
`src/Apps/Apis`, `src/Apps/Jobs` and `src/Apps/Ui`; `--layout single-app` puts them directly under `src` and maps one
application folder to the `Apps` layer. `--no-tests` skips the test projects, `--with-mock-server` adds the mock
server host, and `--isolated-app-settings` gives each application its own settings instead of the shared ones. The folder defaults to `App`;
choose names such as `Api`, `Job`, `Cli` or the solution name with `--application-folder`. Use `--layering prefix` to
choose project-name prefixes without the wizard; use
`--save-preferences` only when the selected preference should be persisted. Otherwise bootstrap and scaffold use
operation arguments and defaults without creating or changing `.anarchy-toolkit.json`. Use the wizard when you need
custom layers. For saved defaults and editor commands, see [Automation Preferences](../guides/automation-preferences.md).

### What you get

```mermaid
flowchart LR
    subgraph Solution["MyApp/"]
        direction TB
        Libs["src/Library/&lt;Layer&gt;/<br/>one library per layer"]
        Apps["src/Apps/Apis · src/Apps/Jobs · src/Apps/Ui<br/>your applications"]
        Tests["tests/Unit · Integration · EndToEnd · Common<br/>optional tests/MockServer"]
        Build["build/_build.csproj · build/Build.cs<br/>Anarchy build host"]
        Tooling["Makefile · scripts/<br/>.fallout/parameters.json · optional .anarchy-toolkit.json"]
        Props["Directory.Packages.props<br/>central package versions"]
    end
    Apps --> Libs
    Tests --> Apps
    Build -.->|"compile · test · check_layering"| Libs
```

| Kind | Built on | Starts with |
|---|---|---|
| API | [`Runtime.Web`](../packages/Runtime.Web/README.md), plus [`Runtime.Web.Swagger`](../packages/Runtime.Web.Swagger/README.md) unless left out (`--no-swagger`, `scaffolding.swagger`) | A REST API with JWT authorization and a `/ping` endpoint. |
| Job | [`Runtime`](../packages/Runtime/README.md) | A job entry point (a local worker or a one-off run) and a service. |
| UI | [`Runtime.Web.Ui`](../packages/Runtime.Web.Ui/README.md) | A Blazor UI with cookie-session login and a home page. |

APIs, jobs and UIs all belong to the `Apps` layer. Only applications with an AWS Lambda entry point reference the AWS packages ([`Runtime.Web.Aws`](../packages/Runtime.Web.Aws/README.md) for an API, [`Runtime.Aws`](../packages/Runtime.Aws/README.md) for a job). Namespaces start with the solution name (e.g. `MyApp.Apps.Apis.Orders`), the solution-level files are listed in a `solution items` solution folder, and no `samples` folder is created. Package versions are managed centrally in `Directory.Packages.props`, and existing files are never overwritten.

## Build and run

```bash
cd MyApp
make install        # set up the local .NET environment, restore and compile
make check_layering # verify that dependencies only point down the layers
make test           # run the tests
make help           # list every target
```

Review each application's settings (auth service URLs, client IDs and scopes) before running it, and use `make appsettings` to generate the untracked `appsettings.Development.json` files.

## Add applications later

From anywhere inside the solution:

```bash
anarchy-toolkit scaffold          # guided
anarchy-toolkit scaffold api Users
anarchy-toolkit scaffold job ClientSync
anarchy-toolkit scaffold ui UserAdmin
anarchy-toolkit scaffold library Reports --layer Infrastructure
anarchy-toolkit scaffold tests unit
```

The tool places each application from explicit arguments, optional saved preferences and then the standard in-code folder baseline; a preferences file is not required and the tool does not infer a layout. Pass `--save-preferences` to persist selected choices. It adds the application to the solution and references the toolkit package it needs, following the solution's central package management.

## Agent skills

The toolkit ships agent skills that teach coding agents (Claude Code, OpenCode, Codex and others that read `.agents/skills`) how to bootstrap, scaffold, build, test, run, release and maintain the solution: `anarchy` is the entry point, `anarchy-conventions` holds the recommended coding conventions, and `anarchy-develop`, `anarchy-environment`, `anarchy-release` and `anarchy-maintenance` cover each area. They are opt-in; add them to an existing solution with:

```bash
anarchy-toolkit skills install   # or: make install_skills
```

Mark your own make targets with `#@ <skill>`, then `make skills_sync` creates their skills from a template and `make skills_validate` checks that the skills are complete and up to date (see [Agent skills](../packages/Automation.Build/README.md#agent-skills)). To let an agent adopt the toolkit for you, see [Let your agent do it](README.md#let-your-agent-do-it).

## Next steps

- Learn what the generated build can do in [Adding Anarchy automation](automation.md) and the [Anarchy Automation Builds guide](../guides/automation-builds.md).
- Follow the [Layered Architecture guide](../guides/architecture.md) as the solution grows.
- The full reference, including the layering options, is the [`Automation.Build.Cli` package page](../packages/Automation.Build.Cli/README.md).
