# 0003. One validated settings object per application

## Context

Settings bound lazily through [`IOptions<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptions-1) fail on the first request that reads a missing or invalid value, often in production, long after the deployment succeeded.

## Decision

An application describes all its settings in one `AppSettings` type with [DataAnnotations](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations), registered through `ModuleAppSettingsBase<TAppSettings>` with validation on start ([`ValidateOnStart`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.optionsbuilderextensions.validateonstart)). Modules get their parts through `c => c.Get<AppSettings>()!.Section`. Local values come from an `appsettings.json.template` and `GenerateAppSettings`.

## Consequences

- A missing section or an invalid value stops the host at start-up, where deployment health checks catch it.
- One type shows everything an application can be configured with.
- Validation attributes must hold in every environment, which needs checking when adopting it in an existing application.

## Alternatives considered

- **`IOptions<T>` per section with `ValidateOnStart`.** Equivalent safety, scattered across registrations; still fully supported.
