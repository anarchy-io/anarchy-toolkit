# Anarchy.Toolkit.Runtime.Web.Ui

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

An interactive-server Blazor UI pipeline with reusable components and cookie-session OAuth login.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Web.Ui
```

- Derive from `UiWebAppStartupBase` and supply the application's environment catalog through `UiAuthenticationOptions.Environments`.
- Use `UiApiClientBase` for downstream API clients and the supplied login, notification, confirmation, JSON and pagination components for UI composition.
- Pages, API clients and environment URLs belong to the consuming application.

## When to use it

Use this package for a server-interactive Blazor WebApp, with or without a login. `AddToolkitUi` runs on a plain `WebApplication`; the cookie session add-on (`AddCookieSession`) signs in with OAuth client credentials and calls environment-specific downstream APIs. It provides shared UI infrastructure and components, not your application's pages, API contracts or identity server.

## How to use it

1. Call `services.AddToolkitUi()`, `app.UseToolkitUi()` and `endpoints.MapToolkitUi()`, or derive a startup from `UiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>`.
2. For a login, opt into the session with `AddToolkitUi(ui => ui.AddCookieSession(options))` (or by overriding `GetUiAuthenticationOptions`), providing `UiAuthenticationOptions`, including the required OAuth scope and an environment catalog with auth and API URLs. The deployed host's environment must be present in that catalog.
3. Register `AddToolkitUiApiHttpClient`, your own clients and presenters. Derive a client from `UiApiClientBase` to resolve the target environment and send authenticated calls.
4. Map your Razor application with interactive server rendering, then compose pages and layouts with the supplied login, session, notification, modal and pagination components. Keep real credentials and deployment URLs in appropriate configuration or secret stores.

The [UI guide](https://anarchy-io.github.io/anarchy-toolkit/guides/building-uis.html#wiring-a-ui) includes startup and client examples; the [sample startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Ui/Composition/SampleUiStartup.cs) also demonstrates a local stub setup.

See the [UI guide](https://anarchy-io.github.io/anarchy-toolkit/guides/building-uis.html), [UI sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Ui) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Theme

`css/toolkit-ui.css` follows the documentation sites and the Anarchy brand guide: neutral surfaces, a black header with the red/orange gradient rule, red/orange kept for primary actions and important states, and Inter and JetBrains Mono when installed (system fonts otherwise; nothing is downloaded).

- **Light and dark mode.** Every colour token is a `light-dark(<light>, <dark>)` pair. Pages follow the system preference; add `<ThemeToggle />` (for example to the navbar) to let users switch, and their choice is remembered in the browser. `<html data-theme="light">` or `"dark"` forces a mode.
- **Assets.** Served from the usual `wwwroot` layout: `css/toolkit-ui.css`, `js/toolkit-ui.js`, and from `Anarchy.Toolkit.Runtime.Web` `favicon.svg`, `favicon.png` and `images/logo-dark.png`.
- **Login logo.** `LoginForm` shows the Anarchy I/O logo above the heading, `images/logo.png` in light mode and `images/logo-dark.png` in dark mode. Set `BrandLogoPath` and `DarkBrandLogoPath` to use your own, or `BrandLogoPath=""` to show none.
- **Logo.** `images/logo-dark.png` is the Anarchy I/O logo with white text (for dark backgrounds), sized for the dark navbar with the `navbar-logo` class (as in the sample and scaffolded layouts).
- **Rebranding.** Override the tokens at the top of `css/toolkit-ui.css` (`--primary-color`, `--link-color`, `--header-bg`, `--brand-gradient`, …) in a stylesheet passed to `UiHeadAssets`' `Stylesheets`, which load after the toolkit's.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
