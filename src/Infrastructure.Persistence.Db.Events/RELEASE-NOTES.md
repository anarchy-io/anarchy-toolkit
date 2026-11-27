## Releases

### 2.0.0

- **Breaking:** types follow the new `Core` and `Infrastructure.Persistence.Db` namespaces.
- **Breaking:** depends on the new major versions of `Core.Events` and `Infrastructure.Persistence.Db`.

### 1.0.0

- Introduce relational event-store repository bases, Outbox contracts and records, a type identifier resolver, and a rollback handler.
- Support round-tripping event custom attributes through consumer-owned SQL as a JSON object.
