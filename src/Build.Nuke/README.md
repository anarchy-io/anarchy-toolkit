# Anarchy.Toolkit.Build.Nuke

Reusable [NUKE](https://nuke.build) build components for .NET repositories.

```bash
dotnet add package Anarchy.Toolkit.Build.Nuke
```

- Implement components such as `IStandardBuild` and `INuGetBuild` on your NUKE build class for standard build, test, pack and publish targets.
- Optional components cover containers, AWS Lambda, CodeDeploy, dependency maps, layering checks and Slack failure reporting.
- Test targets support Microsoft.Testing.Platform as well as VSTest. The package includes a `buildTransitive` targets file for SDK-provided NuGet assemblies.

See the [NUKE builds guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/nuke-builds.md) for setup, component details and package publishing, and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
