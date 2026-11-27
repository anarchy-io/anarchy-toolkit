# Anarchy.Toolkit.Runtime.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

AWS Lambda hosting for toolkit jobs and event handlers.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Aws
```

- Derive Lambda functions from `LambdaJobEntryPointBase<TStartup>`; the host is built once per execution environment.
- Start each invocation with `BeginInvocation()`, which stores the invocation's X-Ray trace id and opens a service scope.

## When to use it

Use this package for jobs deployed as AWS Lambda functions. Local jobs and workers only need `Anarchy.Toolkit.Runtime`.

## How to use it

```csharp
public sealed class EntryPointLambda : LambdaJobEntryPointBase<JobStartup>
{
    public async Task HandleAsync()
    {
        await using var scope = BeginInvocation();
        await scope.ServiceProvider.GetRequiredService<JobService>().RunAsync("scheduled");
    }
}
```

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
