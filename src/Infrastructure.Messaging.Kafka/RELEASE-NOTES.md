## Releases

### 2.0.0

- **Breaking:** extension classes take the `Extensions` suffix under `Extensions/<extended namespace>/`, helpers live in `Helpers/<Subject>/`, and their namespaces follow those folders: update `using` directives.
- **Breaking:** the Kafka header exceptions move to `Helpers/MessageBroker/Exceptions/`.
- **Breaking:** depends on the new major versions of `Core` and `Core.Messaging.Abstractions`.

### 1.0.0

- Introduced a Kafka message publisher, inbound mapping and dependency-injection module.
