# Anarchy.Toolkit.Web.Ui

An interactive-server Blazor UI pipeline with reusable components and cookie-session OAuth login.

```bash
dotnet add package Anarchy.Toolkit.Web.Ui
```

- Derive from `UiWebAppStartupBase` and supply the application's environment catalog through `UiAuthenticationOptions.Environments`.
- Use `UiApiClientBase` for downstream API clients and the supplied login, notification, confirmation, JSON and pagination components for UI composition.
- Pages, API clients and environment URLs belong to the consuming application.

## When to use it

Use this package for a server-interactive Blazor back-office UI that signs in with OAuth client credentials, maintains a cookie session and calls environment-specific downstream APIs. It provides shared UI infrastructure and components, not your application's pages, API contracts or identity server.

## How to use it

1. Define your settings module and derive a startup from `UiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>`.
2. Provide `UiAuthenticationOptions`, including the required OAuth scope and an environment catalog with auth and API URLs. The deployed host's environment must be present in that catalog.
3. Register `AddToolkitUiApiHttpClient`, your own clients and presenters. Derive a client from `UiApiClientBase` to resolve the target environment and send authenticated calls.
4. Map your Razor application with interactive server rendering, then compose pages and layouts with the supplied login, session, notification, modal and pagination components. Keep real credentials and deployment URLs in appropriate configuration or secret stores.

The [UI guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/building-uis.md#wiring-a-ui) includes startup and client examples; the [sample startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Ui/Composition/Startup.cs) also demonstrates a local stub setup.

See the [UI guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/building-uis.md), [UI sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Ui) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.6

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

### 1.0.5

- chore(deps): update AWS SDK packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced Blazor UI hosting, session handling, presentation services, HTTP client infrastructure and reusable components.
