# 0005. xUnit v3 on Microsoft.Testing.Platform

## Context

xUnit v2 and [VSTest](https://github.com/microsoft/vstest) are in maintenance. xUnit v3 test projects are executables on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) (MTP): they start faster, run without an adapter, and support cancellation through `TestContext.Current.CancellationToken`.

## Decision

The toolkit's tests and `Testing.*` kits use xUnit v3 on MTP, and the boilerplate's [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json) selects the MTP runner for `dotnet test`. [Fallout](https://fallout.build/)'s test targets detect each project's platform and still run VSTest projects.

## Consequences

- The kits require xUnit v3 (and [Moq](https://github.com/devlooped/moq) 4.21 or later, [AwesomeAssertions](https://awesomeassertions.org/)).
- With the MTP runner set in `global.json`, `dotnet test` refuses VSTest projects, so mixed solutions keep it unset until the last project moves.
- Analyzers such as [xUnit1051](https://xunit.net/xunit.analyzers/rules/xUnit1051) apply under warnings as errors.

## Alternatives considered

- **Stay on xUnit v2.** No migration cost, but no future features and a deprecated runner.
