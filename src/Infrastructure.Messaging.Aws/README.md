# Anarchy.Toolkit.Infrastructure.Messaging.Aws

AWS SNS and SQS publishers implementing the toolkit's message-broker abstractions.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Messaging.Aws
```

- Register publishers with `AddSnsMessageBroker` or `AddSqsMessageBroker` and `AwsMessagingOptions`, or use `ModuleSnsMessageBroker` and `ModuleSqsMessageBroker` with configuration-based options factories.
- The package also provides shared publisher and request-builder bases, inbound mapping and compression.
- For integration tests, see [Testing.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Aws) and its LocalStack helpers.

See the [registration example](https://github.com/anarchy-io/anarchy-toolkit#consuming-a-package) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
