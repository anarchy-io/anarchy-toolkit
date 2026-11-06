# Anarchy.Toolkit.Web.Ui

An interactive-server Blazor UI pipeline with reusable components and cookie-session OAuth login.

```bash
dotnet add package Anarchy.Toolkit.Web.Ui
```

- Derive from `UiWebAppStartupBase` and supply the application's environment catalog through `UiAuthenticationOptions.Environments`.
- Use `UiApiClientBase` for downstream API clients and the supplied login, notification, confirmation, JSON and pagination components for UI composition.
- Pages, API clients and environment URLs belong to the consuming application.

See the [UI guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/building-uis.md), [UI sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Ui) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
