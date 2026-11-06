# Anarchy.Toolkit.Testing.Aws

AWS messaging helpers for SNS/SQS integration tests, typically against LocalStack.

```bash
dotnet add package Anarchy.Toolkit.Testing.Aws
```

- Use `SqsHelper` and `AwsMessageBrokerHelper` to work with test queues and messaging resources.
- `SqsMessageFixture` builds SQS messages, including gzip/zlib-compressed payloads.
- Supply a suitable AWS or LocalStack endpoint and resources for your integration tests.

See [Infrastructure.Messaging.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Messaging.Aws), the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
