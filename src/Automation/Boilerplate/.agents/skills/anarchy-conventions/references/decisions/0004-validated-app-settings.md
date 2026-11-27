# 0004. One validated settings object, shared by a monorepo's applications by default

## Context

Settings bound lazily through [`IOptions<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptions-1) fail on the first request that reads a missing or invalid value, often in production, long after the deployment succeeded.

## Decision

An application describes all its settings in one `AppSettings` type with [DataAnnotations](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations), registered through `ModuleAppSettingsBase<TAppSettings>` with validation on start ([`ValidateOnStart`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.optionsbuilderextensions.validateonstart)). Modules get their parts through `c => c.Get<AppSettings>()!.Section`. Local values come from an `appsettings.json.template` and `GenerateAppSettings`. `ModuleAppSettingsBase` always registers the settings as `IAppSettings` too, so the toolkit's own components resolve them; capability interfaces such as `IAuthServiceAppSettings` are optional, and features that need them (REST API authentication, the Swagger OAuth2 scheme) only turn on when the settings implement them.

In a monorepo the applications share one settings object by default: `AppSettings` lives in the Infrastructure library, `ModuleAppSettings` and the `appsettings*.json` files in the Bootstrap library, and the root `Directory.Build.props` copies those files into every project outside the libraries and the build project. A solution can keep one settings object per application instead (`scaffolding.appSettings: isolated`); single-application layouts always do. Scaffolding follows the saved choice, which bootstrap records when it creates shared settings.

## Consequences

- A missing section or an invalid value stops the host at start-up, where deployment health checks catch it.
- One type shows everything an application can be configured with; with shared settings, everything the whole solution can be configured with, in one place.
- Shared settings mean one validation for all applications: a value only one application needs must still be valid (or optional) for the others.
- Validation attributes must hold in every environment, which needs checking when adopting it in an existing application.

## Alternatives considered

- **`IOptions<T>` per section with `ValidateOnStart`.** Equivalent safety, scattered across registrations; still fully supported.
- **Always one settings object per application.** Kept as an option; as a default it duplicated the same sections and files across every application of a monorepo.
