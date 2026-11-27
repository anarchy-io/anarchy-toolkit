# 0006. Kafka as a second messaging transport

## Context

The messaging contracts should support a broker beyond AWS SNS and SQS. A second adapter exposes assumptions that are easy to miss when there is only one implementation. The user chose Kafka for the optional second transport.

## Decision

Provide a Kafka publisher and inbound mapper in `Infrastructure.Messaging.Kafka` and a matching `Testing.Kafka` kit. They depend on the broker-neutral contracts and the Kafka client, not AWS, CloudEvents or the outbox. The application supplies broker configuration and owns the consumer loop and offset policy. The contract uses an opaque broker reference rather than the SQS-specific receipt-handle name, and a deduplication ID is optional.

## Consequences

- Kafka topic, partition, offset and key semantics are handled in the Kafka package; SNS/SQS remain separate.
- Kafka commits offsets, not individual receipt handles. An inbound reference identifies a location but does not itself acknowledge a message.
- Kafka does not enforce the application's deduplication ID. Producer idempotence does not deduplicate separate application-level publishes; consumers and applications must design for retries and duplicates.
- The publisher treats each record as a chunk, so callbacks and failures are per record, not transactional batches. Cancellation and failures can leave delivery uncertain.
- The Core contract rename is a public breaking change requiring a major package release and dependent-package handling; making the deduplication ID optional changes validation. The new packages start at `1.0.0`.
- The test kit supports fixtures and a real consumer, but broker-backed integration tests need an independently provisioned Kafka-compatible broker.

## Alternatives considered

- **RabbitMQ.** A vendor-neutral queue with different routing and acknowledgements; valid, but Kafka was explicitly selected and additionally exercises log offsets and partition keys.
- **Defer the second transport.** Avoids another client and maintenance burden, but leaves the broker-neutral contract unproven outside AWS.
