# Enum Conventions

This guide explains when enums are a great fit in code built on the toolkit, and when they quietly become a coupling trap. 🪄

## Why this matters

Enums look clean, but they come with a hidden contract: every possible value must already be known in code.

That is perfect for **local, controlled vocabularies**.
It is risky for **data coming from external systems**, and for **large datasets where string-based storage or filtering is expensive**.

The short version:

- use enums for **local decisions, comparisons, defaults and fixtures**,
- avoid enums for **parsing uncontrolled external values**,
- prefer **auxiliary tables with integer IDs** when values must stay flexible or cheap to query.

This is also why the toolkit's generic code is **string-based at its core**: `Event.TypeAggregate`/`TypeEvent`, `SupportedAggregateTypes`, message destinations and so on are strings. Consumers bring their own enums and convert them at the edge with `ToDescription()` or the enum-friendly overloads (for example `Event.Create<TAgg, TEvt>(...)` or `SupportedAggregateTypes.Support<TAgg, TEvt>(...)`).

## ✅ When an enum is a good fit

Use an enum when all valid values are owned by your application and are expected to stay intentionally small and explicit.

Typical good uses:

- business-rule comparisons,
- default property assignment,
- test fixture / object initialization,
- stable internal states that you fully control.

Example:

```csharp
StatusSummary = OrderStatusSummary.Created.ToDescription();
```

That is safe because the enum is being used as a **local constant source**, not as a parser for unknown input.

## 🚫 When to avoid an enum

Avoid enums when values come from a source you do not control, such as:

- external APIs,
- database content that can evolve independently,
- upstream systems that may introduce new values without coordinating with you.

Parsing those values directly into an enum means your application breaks whenever the source sends a value you have not deployed yet.

## 🧰 The auxiliary-table pattern

For flexible string attributes you want to persist without duplicating text everywhere, use auxiliary tables (lookup tables with an integer id and a unique name). The outbox package does this for its event attributes, through `EventTypeTable.TypeAggregate`, `TypeEvent` and `TypeTask`.

At write time, the application keeps working with readable string values at the boundary, but stores a numeric foreign key in the main table.

Under the hood, `AttributeIdCacheBase<TAttributeTypes, TId>` (`Anarchy.Toolkit.Infrastructure.Persistence.Db`) keeps a small in-memory cache per auxiliary-table type:

1. on first access, it hydrates all known values for that auxiliary table,
2. it keeps a `name -> id` map in memory for fast lookups,
3. if a value is missing, it inserts that single value and caches the returned id.

You get the flexibility of strings without paying the storage and lookup cost repeatedly in the main data tables. Derive from `AttributeIdCacheBase` for your own auxiliary tables (see `EventWriterMultiAttributeIdCache` and its `PostgresEventWriterMultiAttributeIdCache` / `OracleEventWriterMultiAttributeIdCache` subclasses for an example).

### 🔄 Why the cache is cleared on rollback

There is one important edge case: a transaction can insert a new auxiliary-table row, cache the generated id, and then roll back.

If the cached id were kept, future writes could reuse an identifier that no longer exists and trigger foreign-key failures.

To prevent that, the outbox's `RolledBackTransactionEventHandler` listens for the unit of work's `RolledBackUowTransactionLocalEvent` and calls `Clear()` on the relevant caches. The next lookup re-hydrates from the database and rebuilds a consistent cache. Do the same for any cache of your own that can be populated inside a transaction.

## ⚡ Event attributes: why auxiliary tables are the right fit

Event tables grow very large, so filtering or indexing on long strings costs more than filtering on compact integer ids.

Auxiliary tables let you:

- keep a canonical string value in a dedicated lookup table,
- store only integer foreign keys in the large event tables,
- run quick lookups and filters on simple indexed numeric columns instead of repeatedly indexing and comparing strings.

The application stays expressive at the edges, while persistence stays lean and query-friendly.

## 📦 External, uncontrolled values

When a value is sourced from a system you do not control (an order status from another team's service, for example), new values can appear before your application is updated.

So the value should not be treated as a strict enum for persistence or parsing. Instead:

- accept the incoming status string as data,
- store it through an auxiliary table,
- keep an enum only for local comparisons and manual assignments.

The key rule is: **do not force external, uncontrolled values through an enum parser**.

## 🛡️ External, but explicitly mapped

Sometimes an external value is intentionally **translated** into a small local vocabulary:

```csharp
return dto?.status?.ToLower() switch {
    "approved" => FraudCheckStatus.Approved,
    "denied" => FraudCheckStatus.Rejected,
    "cancelled" => FraudCheckStatus.Deleted,
    _ => FraudCheckStatus.Pending
};
```

That explicit mapping creates a clear boundary:

- the remote API can speak in its own terms,
- your application chooses which local values exist,
- unexpected values fall back to a controlled local option.

Because the persisted value is **your mapped local status**, keeping the enum in code (and even a database enum) is acceptable here.

## 🏷️ `Description`-based enum mapping

When you do use enums, the toolkit relies on the `[Description(...)]` attribute to define the persisted or serialized representation.

`EnumExtension` (`Anarchy.Toolkit.Core`) provides:

- `ToDescription()` to convert an enum value to its description string,
- `ToEnumValue<T>()` / `TryToEnumValue<T>()` to map a description back to an enum,
- `GetAllDescriptions<T>()` to enumerate all declared descriptions.

The same convention is wired into serialization and persistence:

- `DescriptionEnumJsonConverter<TEnum>` / `DescriptionEnumJsonConverterFactory` (System.Text.Json, `Anarchy.Toolkit.Core`),
- `DescribableEnumJsonConverter` (System.Text.Json factory registered by the Web presentation pipeline),
- `DescriptionEnumTypeHandler<TEnum>` (Dapper, provider-agnostic, in `Anarchy.Toolkit.Infrastructure.Persistence.Db`).

These are convenient for **stable local mappings**. They should not be used as blanket parsers for arbitrary upstream data you do not own.

## 🧭 Rule of thumb

Before introducing or keeping an enum, ask:

1. **Do we own the vocabulary?**
2. **Can new values appear without a deployment on our side?**
3. **Is this value used mainly for local decisions, or for persisting external data?**

Choose the pattern like this:

- **Enum**: controlled local vocabulary.
- **Manual mapping into an enum**: external source, intentionally translated into our own bounded local vocabulary.
- **Auxiliary table**: external or evolving vocabulary that must be accepted without tightly coupling the application to every possible value.

If in doubt, prefer the option that keeps the boundary explicit and avoids letting external systems define your application's runtime behavior.
