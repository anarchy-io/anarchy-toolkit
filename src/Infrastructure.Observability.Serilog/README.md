# Anarchy.Toolkit.Infrastructure.Observability.Serilog

Serilog enrichers and CloudWatch integration for services, including AWS EC2 workloads.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Observability.Serilog
```

- Enrich log events with `WithTeam`, `WithAppGroup`, `WithCostCenter`, `WithCostCenterFromEnvironment` and `WithLogRetention`; property names can be overridden.
- Use `WithAwsEc2InstanceId` for EC2 instance metadata and the `Ec2CloudWatch(logGroup)` sink for CloudWatch logging.
- Configure the enrichers and sink in your own Serilog setup or observability factory.

## When to use it

Use this adapter when a service already uses Serilog and needs consistent organizational log properties or CloudWatch logging from EC2. Core instrumentation can remain dependent on `Core.Observability` rather than on this AWS-specific package.

## How to use it

Add the desired enrichers to the host's `LoggerConfiguration`, or override `ObserverFactory.Enrich` when using the toolkit configuration bootstrap. For example, the observability guide shows an override chaining `WithTeam("payments")` and `WithCostCenter("finance")` onto the base configuration.

For an EC2-hosted CloudWatch sink, configure `Ec2CloudWatch(logGroup)` in the host's sink setup and provide the necessary AWS credentials, region and CloudWatch permissions. Avoid adding an EC2-specific sink or metadata enricher to environments where they are not appropriate. The package does not create an observability backend or define your retention policy for you.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to NUKE

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced Serilog configuration extensions and enrichers, including AWS EC2 instance ID enrichment.
