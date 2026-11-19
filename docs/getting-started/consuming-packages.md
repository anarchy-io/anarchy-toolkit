# Using the Packages

The platform's **Infrastructure** pillar is a set of packages, each published to [NuGet.org](https://www.nuget.org/profiles/anarchy-io) on its own, so an application references only what it uses, without adopting the rest of the platform:

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Messaging.Aws
```

Each package's README (on NuGet.org and in the [Package Catalog](../packages/README.md)) explains when to use it and how to wire it up. The [samples](../samples/README.md) show complete applications.

## Registering services

Each package with concrete services ships a dependency-injection entry point, either an `IDependencyInjectionModule` or an [`IServiceCollection`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) extension, so applications don't wire classes by hand:

```csharp
services.AddLocalEventBus();
services.AddSnsMessageBroker(new AwsMessagingOptions("eu-west-1", "123456789012"));
services.AddSqsMessageBroker(new AwsMessagingOptions("eu-west-1", "123456789012"));
services.AddPostgresHealthReports();
```

or, as modules, with options read from the application's own configuration:

```csharp
services
    .UseModule(
        new ModuleSnsMessageBroker(config => new AwsMessagingOptions(config["Aws:Region"]!, config["Aws:AccountId"]!)),
        configuration,
        environment)
    .UseModule<ModulePostgresHealthReport>(configuration, environment);
```

Modules never assume a configuration section or settings type: options always come from the application. See [Dependency-injection modules](../guides/architecture.md#dependency-injection-modules) for the modules each package ships.

## Picking packages by workload

| You are building | Start with |
|---|---|
| A REST API (Kestrel or [AWS Lambda](https://aws.amazon.com/lambda/)) | [`Web`](../packages/Web/README.md), plus [`Testing.Web`](../packages/Testing.Web/README.md) for tests |
| A GraphQL API | [`Web.GraphQL`](../packages/Web.GraphQL/README.md) |
| A [Blazor](https://learn.microsoft.com/aspnet/core/blazor/) WebApp | [`Web.Ui`](../packages/Web.Ui/README.md) and the [Building UIs guide](../guides/building-uis.md) |
| An AWS Lambda job | [`Hosting`](../packages/Hosting/README.md) |
| An EC2/systemd worker | [`Hosting.Workers`](../packages/Hosting.Workers/README.md) |
| Relational persistence | A provider: [`Postgres`](../packages/Infrastructure.Persistence.Db.Postgres/README.md), [`Oracle`](../packages/Infrastructure.Persistence.Db.Oracle/README.md) or [`Mysql`](../packages/Infrastructure.Persistence.Db.Mysql/README.md). See [Database Providers](../guides/database-providers.md) |
| Messaging over SNS/SQS | [`Infrastructure.Messaging.Aws`](../packages/Infrastructure.Messaging.Aws/README.md) |
| A new solution from scratch | The [`anarchy-toolkit` tool](../packages/Automation.Build.Cli/README.md) and [`Automation.Build`](../packages/Automation.Build/README.md) |

Every host registers [`Configuration`](../packages/Configuration/README.md)'s `ModuleLibConfiguration`, which brings settings binding, secrets and the [observability](../guides/observability.md) bootstrap in one module.

## Plugging in your organization's conventions

Organization-specific behavior is not part of the toolkit. Organizations plug their conventions into its extension points instead: an overridable `ObserverFactory` for sinks, log properties and metrics backends; options and constructor parameters; and `ConfigureAppConfiguration` hooks for configuration sources such as the AWS Parameter Store. See [Plugging in an organization's setup](../guides/observability.md#plugging-in-an-organizations-setup).

Domain code (task orchestration, concrete repositories and clients, domain event payloads, message destinations, Swagger scopes, UI environment URLs) belongs in the consuming applications, not in the toolkit. Generic code takes strings and generic parameters instead of domain enums; enum overloads convert values with `EnumExtension.ToDescription()` (see [Enum Conventions](../conventions/enums.md)). Entities are marked with `IEntity`/`IPersistedEntity`.

## Assertions in tests

The toolkit uses **[AwesomeAssertions](https://awesomeassertions.org/)** rather than FluentAssertions. Only the `Anarchy.Toolkit.Testing*` packages depend on it, so production packages bring no assertion library into your application.
