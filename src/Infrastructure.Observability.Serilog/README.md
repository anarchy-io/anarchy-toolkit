# Anarchy.Toolkit.Infrastructure.Observability.Serilog

Serilog enrichers and CloudWatch integration for services, including AWS EC2 workloads.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Observability.Serilog
```

- Enrich log events with `WithTeam`, `WithAppGroup`, `WithCostCenter`, `WithCostCenterFromEnvironment` and `WithLogRetention`; property names can be overridden.
- Use `WithAwsEc2InstanceId` for EC2 instance metadata and the `Ec2CloudWatch(logGroup)` sink for CloudWatch logging.
- Configure the enrichers and sink in your own Serilog setup or observability factory.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
