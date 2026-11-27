# Anarchy.Toolkit.Infrastructure.Messaging.Kafka

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/) · **Pillar:** Infrastructure · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Apache Kafka publisher and inbound mapper for `Core.Messaging.Abstractions`, using the Confluent .NET client. Works with Kafka-compatible brokers; no AWS, CloudEvents or outbox dependency.

```csharp
using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.DependencyInjection.Module;
using Confluent.Kafka;

services.UseModule(new ModuleKafkaMessageBroker(cfg => new ProducerConfig {
    BootstrapServers = cfg["Kafka:BootstrapServers"],
    EnableIdempotence = true
}), configuration, environment);
// Register AppTelemetry separately, e.g. services.AddCoreTelemetry().
// Resolve [FromKeyedServices("kafka")] IMessageBrokerPublisher.
```

`AddKafkaMessageBroker(config)` is also available. The container owns and disposes the producer; a manually constructed publisher does not own it. Configure authentication, durability, timeout and producer options yourself. Use broker acknowledgement settings appropriate to the application; do not set `Acks=0` when acknowledgement is required.

## Mapping and delivery

Destination is the Kafka topic; body and partition key are UTF-8 string value and key. Custom attributes become UTF-8 headers. `anarchy-message-id` and `anarchy-deduplication-id` are reserved headers; collisions are rejected. The ID is preserved; the optional deduplication ID is carried but **not enforced** by Kafka. Producer idempotence does not deduplicate separate application-level publishes. Inbound mapping uses the ID header, or `topic:partition:offset` when absent; `BrokerReference` always contains that opaque location. It rejects duplicate, null or invalid UTF-8 headers, tombstones and empty bodies. The mapper strips its two reserved headers from attributes.

Inbound invalid headers throw `KafkaHeaderMappingException`, with specific duplicate, null-value and invalid-encoding subtypes and the offending `HeaderName`. These remain catchable as `ArgumentException`; invalid UTF-8 retains the original `DecoderFallbackException` as its inner exception. Because the .NET decoder exception is sealed, it cannot also be the base of the specialized mapping exception. Reserved or null outbound attributes and partition-end notifications have their own argument exception types.

Call `KafkaMessageMapper.FromKafka(consumeResult)` in your own consumer loop. Offset management, committing only after successful processing, retry and poison-message policy are the application's responsibility. Offset commits are not per-message acknowledgements, and Kafka may redeliver. There is no consumer loop, transaction, outbox, or exactly-once guarantee in this package.

Batch publishing sends one record per chunk in input order, including across topics. Before/after callbacks run once per attempted record except when cancellation interrupts the attempt; a before failure skips that record, and an after failure is reported as a chunk failure even if delivery succeeded. `continueOnFailure` continues with subsequent records and can break ordering. Cancellation propagates; a cancelled or timed-out send may still reach Kafka, so retry using application-level idempotency. Empty batches do nothing. The matching `Testing.Kafka` kit provides a fixture and a real consumer for broker-backed tests.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
