# Anarchy.Toolkit.Bootstrap.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Bootstrap · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Opt-in AWS configuration for toolkit applications: AWS settings, KMS secret management, Secrets Manager values, the SSM Parameter Store and the X-Ray trace id context.

```bash
dotnet add package Anarchy.Toolkit.Bootstrap.Aws
```

- `IAwsAppSettings`/`AwsSettings` bind the `Aws` settings section.
- `ModuleAwsSecretManagement` decrypts secrets with AWS KMS in remote environments; pass it to `ModuleLibConfiguration.WithSecretManagement`.
- `IHostEnvironment.GetAwsSecretValue` reads AWS Secrets Manager remotely; `AddAwsSystemsManagerParameterStore` adds the SSM Parameter Store as a configuration source.
- `AmazonRequestContext`/`IAmazonRequestContext` (`ModuleAwsRequestContext`) expose the X-Ray trace id of the current request.

## When to use it

Use this package only in applications that run on AWS. `Anarchy.Toolkit.Bootstrap` stays provider-neutral: without this package no AWS SDK is referenced and no remote secret service is registered.

## How to use it

```csharp
services.UseModule(
    new ModuleLibConfiguration<ModuleAppSettings, AppSettings>()
        .WithSecretManagement(new ModuleAwsSecretManagement()),
    configuration, environment);
```

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
