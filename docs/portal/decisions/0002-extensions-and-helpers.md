# 0002. Extensions and helpers in dedicated folders, with namespaces that follow them

## Context

The toolkit's packages kept extension methods in singular `Extension/` folders, with namespaces mirroring those folders (`….Extension.Microsoft.DependencyInjection`), and mixed pure helpers, process runners, exceptions and converters in `Helper/` folders. Callers needed a different `using` per extended type, and the code did not follow the toolkit's own [code organization convention](../conventions/code-organization.md), which the docs recorded as accepted exceptions.

## Decision

- Extension classes take the `Extensions` suffix and live in a project-root `Extensions/` folder organized by the extended type's namespace (`Extensions/Microsoft/Extensions/DependencyInjection/ServiceCollectionExtensions.cs`).
- Namespaces follow folders, extensions included: an extension of `IServiceCollection` in `Runtime.Web` lives in `Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.Extensions.DependencyInjection`. Extensions never use the extended type's own namespace.
- Static helpers live in `Helpers/<Subject>/` with the namespace `….Helpers.<Subject>`. Exceptions, converters and builders found among extensions or helpers move to their own folders.
- The toolkit follows its conventions without exceptions; deviations are fixed, not documented.

## Consequences

- A file's namespace always follows from its path, with no special case for extensions.
- Callers add one `using` per package and extended namespace; IDEs suggest it from the method name.
- Extensions never leak into, or clash inside, namespaces the toolkit does not own (two packages can each extend `HttpRequestMessage`).
- Published namespaces changed: a breaking change released with the package renames of the same version.
- Namespaces ending in `.System` or `.Microsoft…` shadow the real `System.` and `Microsoft.` roots inside them; code there (and in mirrored test namespaces) uses `using` directives or `global::` instead of fully qualified names.

## Alternatives considered

- **The extended type's namespace (e.g. `Microsoft.Extensions.DependencyInjection`).** Discoverable without a `using`, but every extension leaks everywhere and same-named classes from two packages clash.
- **The package's root namespace for every extension of an outside type.** One `using` per package, but the namespace would no longer follow the folder, the only exception to that rule.
- **Keep the legacy folders and document them as exceptions.** Rejected: the toolkit is the reference implementation of its conventions.
