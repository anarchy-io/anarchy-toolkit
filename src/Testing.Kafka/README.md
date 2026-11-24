# Anarchy.Toolkit.Testing.Kafka

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/) · **Pillar:** Infrastructure · **Layer:** Testing · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Helpers for testing `Infrastructure.Messaging.Kafka`: `KafkaMessageFixture.FromOutbound` makes a consumed record without a broker, while `KafkaTestConsumer` reads a real Kafka-compatible broker from the beginning of a specified partition.

```csharp
using Anarchy.Toolkit.Testing.Kafka.Helper;
using Confluent.Kafka;

// Create a unique topic and publish before reading. Supply your test broker configuration.
using var consumer = new KafkaTestConsumer(new ConsumerConfig {
    BootstrapServers = "localhost:9092", GroupId = Guid.NewGuid().ToString(),
    EnableAutoCommit = false
}, topic);
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var message = consumer.Read(timeout.Token);
```

The helper does not provision or clean up topics, explicitly commit offsets, or start a broker. Disable auto commit, use isolated topics and configure the broker separately. A fixture is useful for unit tests but is not a substitute for a real-broker integration test.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
