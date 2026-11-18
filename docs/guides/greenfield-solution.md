# Your First Solution

A walkthrough from an empty folder to a solution with a shared library per layer, an API, a job, a mock server for its upstreams and a pull-request gate. It follows the path a real team took to start a monorepo for two services on the platform. The [CLI tool page](../getting-started/cli-tool.md) is the reference for every command used here.

## Bootstrap

```bash
dotnet tool install --global Anarchy.Toolkit.Build.Cli.Tool
anarchy-toolkit bootstrap
```

The wizard asks for a name, the layer mapping, the layers and the first applications, and shows the plan before writing anything. Then:

```bash
make install
make pull_request
```

You now have `build/` (the [Fallout](https://fallout.build/) build), a Makefile and scripts, [`Directory.Packages.props`](https://learn.microsoft.com/nuget/consume-packages/central-package-management), a library per layer and your applications, all compiling and tested.

## Choose a layout

The wizard's first real decision is how projects map to layers.

| Layout | Looks like | Pick it when |
|---|---|---|
| **By folder** (default) | `src/Library/Core/Core.csproj`, `src/Library/Web/Web.csproj`, `src/Api/Orders/...` | One or several applications share a library per layer; few, larger projects. |
| **By name prefix** | `src/Core.Orders/`, `src/Infrastructure.Orders.Postgres/` | Many small, separately packaged projects (like the toolkit itself). |

With folder layering, set where the libraries are and which layer each application folder belongs to, in `.fallout/parameters.json`:

```json
{
  "LayeringByFolder": true,
  "LayeringRoot": "src/Library",
  "LayeringApplicationFolders": ["src/Api=Web", "src/Jobs=Hosting"]
}
```

A job then cannot use web code, and no library can reference an application. Several domains can share one project per layer, side by side in folders (`Core/Orders/`, `Core/Billing/`, `Core/Common/`). See the [Layering convention](../conventions/layering.md) for the rules and how to bend them.

## Add applications

```bash
anarchy-toolkit scaffold api Orders
anarchy-toolkit scaffold job InvoiceCleanup
```

Each application is a thin entry point: a `Program`, a `Startup` choosing modules, and settings. The logic goes in the libraries. Describe the settings once in an `AppSettings` class validated on start, and list the keys a developer must provide in `appsettings.json.template`; `make appsettings` asks for them. See [Dependency Injection](../conventions/dependency-injection.md).

## Stand in for upstreams

Add a runnable project whose name ends in [`MockServer`](https://www.mock-server.com/) (for example `src/MockServer`), referencing `Anarchy.Toolkit.Web.MockServer`, put expectation files under a folder per upstream, and add `IMockServerBuild` to the build:

```json
{ "MockServerExpectations": ["src/MockServer/expectations"] }
```

`make mock_server_start` runs it for local development; `make test_end_to_end_mocked` runs it around the tests. Point the applications' development and test settings at it. The [Mock Server guide](mock-server.md) covers runtime expectations, assertions and recording.

## Bring your company stack

Logging sinks, metrics backend, correlation headers, CodeArtifact or other private feeds, auth: see [Bringing your company stack](bringing-your-company-stack.md). Keep that code in your own `Configuration` and `Infrastructure` libraries, in a folder named after the company, so it is easy to find and to replace.

## Add databases and brokers when you need them

Add a `docker/docker-compose.yml` and `IDockerBuild` (`make start`, `stop`, `destroy`), then the persistence or messaging package and its `Testing.*` kit. See [Database Providers](database-providers.md) and the [Package Catalog](../packages/README.md).

## Wire the pipeline

The pull-request pipeline runs one command:

```bash
make pull_request
```

If you deploy with something the toolkit doesn't ship ([CodeDeploy](https://aws.amazon.com/codedeploy/), Lambda zips, a company deploy tool), keep it in a separate release build next to `build/`, with its own make targets. See [Fallout builds](fallout-builds.md) for packages and container images.

## Write down your deviations

Your solution will differ from the conventions somewhere: an exception middleware with your own mapping, a different test layout, a pinned driver. Record each one in an ADR or in `AGENTS.md`, linking the convention you deviate from. Coding agents and new team members then know which rule wins.

## Checklist

- [ ] `make pull_request` passes on a clean clone.
- [ ] `make check_layering` passes, with application folders mapped.
- [ ] Settings validated on start; `appsettings.json.template` lists the local keys.
- [ ] Upstreams mocked; `ShouldAllHaveBeenMatched` fails on unmocked calls.
- [ ] Company specifics isolated in their own folder.
- [ ] Deviations from the conventions recorded.
