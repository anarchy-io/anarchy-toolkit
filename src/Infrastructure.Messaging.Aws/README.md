# Anarchy.Toolkit.Infrastructure.Messaging.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

AWS SNS and SQS publishers implementing the toolkit's message-broker abstractions.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Messaging.Aws
```

- Register publishers with `AddSnsMessageBroker` or `AddSqsMessageBroker` and `AwsMessagingOptions`, or use `ModuleSnsMessageBroker` and `ModuleSqsMessageBroker` with configuration-based options factories.
- The package also provides shared publisher and request-builder bases, inbound mapping and compression.
- `InboundMessage.BeginCorrelationScope(logger)` is the consumer entry point for correlation: it reads the correlation and attempt IDs from the mapped message's string attributes and opens the provider-neutral scope from `Infrastructure.CorrelationId`.
- For integration tests, see [Testing.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Aws) and its LocalStack helpers.

## When to use it

Use this package at the composition root when application code depends on `IMessageBrokerPublisher` and the deployment uses SNS topics or SQS queues. It implements publishing and inbound message mapping; the application remains responsible for creating destinations, permissions and message consumption.

## How to use it

Register `AppTelemetry` and the required broker adapters. The example uses explicit options; obtain real account and region values from your host configuration:

```csharp
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Microsoft.Extensions.DependencyInjection;

var options = new AwsMessagingOptions("eu-west-1", "123456789012");
services.AddSnsMessageBroker(options);
services.AddSqsMessageBroker(options);
```

Alternatively, register the modules with options factories using `UseModule`. Resolve the keyed `topic` or `queue` publisher and build messages with `OutboundMessageBuilder` from the abstractions package. Configure AWS credentials and permissions for the actual destinations, and plan for broker size limits, batch failures and duplicate delivery.

### Consuming messages with correlation

The package maps inbound messages (`InboundMessageMapper.FromSqs(sqsEvent)`) but does not ship a consumer loop, so nothing propagates correlation IDs on its own: your handler opts in by opening the scope around each message.

```csharp
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker;

foreach (var message in InboundMessageMapper.FromSqs(sqsEvent))
{
    using (message.BeginCorrelationScope(logger))
        await processor.HandleAsync(message, cancellationToken);
}
```

The scope sets the async-flowing `CorrelationContext`, opens a standard `ILogger` scope with the correlation and attempt properties (so every logging provider sees them) and tags the current `Activity`. The attribute names default to `CorrelationId` and `AttemptId`; pass other names when the publisher uses different ones. A missing attribute leaves that ID empty rather than generating one. Publishers do not add the attributes: copy `CorrelationContext.CorrelationId` into the outbound message attributes yourself if downstream consumers need it.

### Structured CloudEvents over SQS and SNS

`AwsCloudEventMapper.ToSqsRequest` and `ToSnsRequest` serialize a CloudEvent as structured CloudEvents JSON and use the configured AWS request builders. Pass an `OutboundMessage` with the destination **name**, message attributes and optional FIFO partition and deduplication IDs; its body is replaced, and its ID is not sent by AWS. The builders derive the queue URL or topic ARN and apply FIFO fields only for `.fifo` destinations. If compression causes the builder to produce an empty body, the bridge throws `AwsCloudEventEmptyBodyException`: SQS and SNS require a nonempty message. With compression disabled, the builder rejects oversized messages with `UncompressedMessageTooLargeException`. Reduce the payload or store it externally instead. Keep the CloudEvent ID and source stable across retries.

Both request types carry the formatter's structured-envelope media type, `application/cloudevents+json` (with any formatter-supplied parameters), in an AWS **String** message attribute named `ContentType`. This is application metadata, not an HTTP content-type header or an AWS-reserved field, and differs from the event's `datacontenttype`, which describes its data. The bridge copies caller attributes and overwrites the exact, case-sensitive `ContentType` key if present; it does not mutate the caller's dictionary. Other casing is a distinct AWS attribute and is not interpreted as the content type. The receive methods expect structured JSON and do not validate or require this attribute. It counts against AWS message-attribute limits; configure consumers and SNS filters accordingly.

```csharp
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

var metadata = new OutboundMessage("outbound-1", "orders.fifo", "placeholder",
    partitionKey: "order-42", deduplicationId: "dedup-1",
    attributes: new Dictionary<string, string> { ["CorrelationId"] = "correlation-1", ["Source"] = "ordering" });
var request = AwsCloudEventMapper.ToSqsRequest(cloudEvent, metadata, new SqsAwsPublisherRequestBuilder(options));
// Send request through the AWS SDK. On receipt:
var received = AwsCloudEventMapper.FromSqs(sqsLambdaRecord);
var eventFromBody = received.CloudEvent;
var brokerMetadata = received.Message;
```

`FromSqs` accepts either a Lambda record or an SDK receive `Message` (the latter also requires the queue ARN or URL). It returns the event alongside the received message, retaining the AWS-assigned ID, receipt handle, queue source, group ID, body and string message attributes. For compressed messages the inbound mapper returns the decompressed body; if it is a JSON-encoded string, the event is decoded from the unwrapped string. `FromSnsNotification` reads an actual SNS Notification JSON document, and `FromSnsSqs` reads that wrapper from an SQS record or SDK message, keeping SNS and SQS metadata separately. For SNS raw delivery use `FromSqs` instead. A publish request is not a received notification; AWS assigns its own SNS and SQS IDs. SNS topic ARN and SQS queue source are transport locations, not the CloudEvent `source`. Known typed CloudEvent extensions can be passed to the receive methods; otherwise the SDK treats unknown extensions as strings.

Only structured JSON is supported, not the CloudEvents binary binding. String broker attributes are carried independently of CloudEvent extensions; binary and number attributes, SNS signatures and timestamps, most SQS system attributes, and received deduplication IDs are not represented in these result models. Request the required SQS message/system attributes when receiving. SNS non-raw envelopes are not signature-verified by this mapper; validate them separately when your trust boundary requires it. Inbound compressed messages may use `CompressionType` and binary `CompressionData` attributes. Size, AWS attribute limits, delivery and deduplication guarantees remain the broker's responsibility.

See the [registration example](https://anarchy-io.github.io/anarchy-toolkit/getting-started/consuming-packages.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [CloudNative.CloudEvents](https://www.nuget.org/packages/CloudNative.CloudEvents.NewtonsoftJson) (with its Newtonsoft.Json formatter) | Sends and receives structured CloudEvents over SNS and SQS (`AwsCloudEventMapper`) | Yes: `AwsCloudEventMapper` takes and returns `CloudEvent` |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
