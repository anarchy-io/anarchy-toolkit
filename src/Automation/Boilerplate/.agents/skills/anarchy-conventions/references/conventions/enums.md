# Enums

This convention explains when to use enums in code built on the toolkit, and when to use a more flexible representation to avoid coupling to external systems.

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](https://anarchy-io.github.io/anarchy-toolkit/contributing/)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.

## Why this matters

Enums compiled into a service turn every new value from another system into a deployment. Packages stay [domain-free](../decisions/README.md#values), so values owned by other systems are strings or auxiliary tables, not toolkit enums.

An enum carries an implicit contract: every possible value must be known at compile time.

This is appropriate for **local, controlled vocabularies**.
It is not appropriate for **data coming from external systems**, and for **large datasets where string-based storage or filtering is expensive**.

In summary:

- use enums for **local decisions, comparisons, defaults and fixtures**,
- avoid enums for **parsing uncontrolled external values**,
- prefer **auxiliary tables with integer IDs** when values must stay flexible or cheap to query.

This is also why the toolkit's generic code is **string-based at its core**: `DomainEventEnvelope.AggregateType`/`Type`, `SupportedAggregateTypes`, message destinations and so on are strings. Consumers bring their own enums and convert them at the edge with `ToDescription()` or the enum-friendly overloads (for example `DomainEventEnvelope.Create<TAgg, TEvt>(...)` or `SupportedAggregateTypes.Support<TAgg, TEvt>(...)`).

## When to use an enum

Use an enum when all valid values are owned by your application and are expected to stay intentionally small and explicit.

Typical uses:

- business-rule comparisons,
- default property assignment,
- test fixture / object initialization,
- stable internal states that you fully control.

Example:

```csharp
Status = UserStatus.Active.ToDescription();
```

This is safe because the enum is being used as a **local constant source**, not as a parser for unknown input.

## When to avoid an enum

Avoid enums when values come from a source you do not control, such as:

- external APIs,
- database content that can evolve independently,
- upstream systems that may introduce new values without coordinating with you.

If such values are parsed directly into an enum, the application fails whenever the source sends a value that has not yet been added to the code.

## The auxiliary-table pattern

For flexible string attributes you want to persist without duplicating text everywhere, use auxiliary tables (lookup tables with an integer id and a unique name). Event persistence uses `EventTypeTable.TypeAggregate` and `TypeEvent` to identify dimensions for type lookup.

At write time, the application keeps working with readable string values at the boundary, but stores a numeric foreign key in the main table.

The event writer resolves each type ID against the caller's database and active transaction, if any. Despite its name,
`EventWriterMultiAttributeIdCache` does not share identifiers between lookups: its callbacks do not expose database
identity or transaction commit outcome, so a shared cache could mix databases or expose an uncommitted ID. Rollback
handler registration is not required for this resolution path. Concurrent first-time registration relies on provider
get-or-insert SQL and database unique constraints to coordinate inserts safely.

For other auxiliary tables, `AttributeIdCacheBase<TAttributeTypes, TId>` (`Anarchy.Toolkit.Core.Persistence.Abstractions.Model`, supporting `string` and `Guid` identifiers as well as numeric ones)
offers a process-wide in-memory cache per auxiliary-table type:

1. on first access, it hydrates all known values for that auxiliary table,
2. it keeps a `name -> id` map in memory for fast lookups,
3. if a value is missing, it inserts that single value and caches the returned id.

This can avoid repeated lookups as well as storing strings in the main data tables. Derive from `AttributeIdCacheBase`
for your own auxiliary tables where sharing a cache is safe, e.g.:

```csharp
public sealed class ClientTypeIdCache : AttributeIdCacheBase<ClientTypeTable, int>
{
    // Hydrate and insert queries for the client_type auxiliary table
}
```

### Transaction and database isolation for caches

The cache is a generic, business-neutral abstraction; event-specific lookups live in `Infrastructure.Persistence.Db.Events`. `AttributeIdCacheBase` shares entries for each closed generic type across the process; it does not partition them by
database or track transaction outcomes. A transaction can hydrate or insert an uncommitted row and publish its ID to
another caller before it rolls back. Clearing the cache on rollback cannot prevent that earlier exposure and can race
with an in-flight lookup.

Use a shared cache only when entries are scoped to the correct database and known to be committed before publication;
otherwise resolve against the caller's database and transaction without sharing IDs. A rolled-back ID must not be reused
by later lookups. In either approach, enforce unique names and make the database get-or-insert operation safe under
concurrent registration.

## Event attributes: why auxiliary tables are the right fit

Event tables grow very large, so filtering or indexing on long strings costs more than filtering on compact integer ids.

Auxiliary tables let you:

- keep a canonical string value in a dedicated lookup table,
- store only integer foreign keys in the large event tables,
- run quick lookups and filters on simple indexed numeric columns instead of repeatedly indexing and comparing strings.

The application works with readable values at its boundaries, while persistence remains compact and efficient to query.

## External, uncontrolled values

When a value is sourced from a system you do not control (for example, a user's account status returned by an external identity provider), new values can appear before your application is updated.

Such a value should not be treated as a strict enum for persistence or parsing. Instead:

- accept the incoming status string as data,
- store it through an auxiliary table,
- keep an enum only for local comparisons and manual assignments.

The rule is: **do not force external, uncontrolled values through an enum parser**.

## External, but explicitly mapped

Sometimes an external value is intentionally **translated** into a small local vocabulary:

```csharp
return dto?.status?.ToLower() switch {
    "verified" => EmailVerificationStatus.Verified,
    "rejected" => EmailVerificationStatus.Failed,
    "expired" => EmailVerificationStatus.Expired,
    _ => EmailVerificationStatus.Pending
};
```

That explicit mapping creates a clear boundary:

- the remote API can speak in its own terms,
- your application chooses which local values exist,
- unexpected values fall back to a controlled local option.

Because the persisted value is **your mapped local status**, keeping the enum in code (and even a database enum) is acceptable here.

## `Description`-based enum mapping

When you do use enums, the toolkit relies on the `[Description(...)]` attribute to define the persisted or serialized representation.

`EnumExtension` (`Anarchy.Toolkit.Core`) provides:

- `ToDescription()` to convert an enum value to its description string,
- `ToEnumValue<T>()` / `TryToEnumValue<T>()` to map a description back to an enum,
- `GetAllDescriptions<T>()` to enumerate all declared descriptions.

The same convention is wired into serialization and persistence:

- `DescriptionEnumJsonConverter<TEnum>` / `DescriptionEnumJsonConverterFactory` (System.Text.Json, `Anarchy.Toolkit.Core`),
- `DescribableEnumJsonConverter` (System.Text.Json factory registered by the `Runtime.Web` presentation pipeline),
- `DescriptionEnumTypeHandler<TEnum>` ([Dapper](https://github.com/DapperLib/Dapper), provider-agnostic, in `Anarchy.Toolkit.Infrastructure.Persistence.Db`).

These are convenient for **stable local mappings**. They should not be used as blanket parsers for arbitrary upstream data you do not own.

## Rule of thumb

Before introducing or keeping an enum, ask:

1. **Do we own the vocabulary?**
2. **Can new values appear without a deployment on our side?**
3. **Is this value used mainly for local decisions, or for persisting external data?**

Choose the pattern like this:

- **Enum**: controlled local vocabulary.
- **Manual mapping into an enum**: external source, intentionally translated into our own bounded local vocabulary.
- **Auxiliary table**: external or evolving vocabulary that must be accepted without tightly coupling the application to every possible value.

When in doubt, prefer the option that keeps the boundary explicit and avoids letting external systems define your application's runtime behavior.

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Use enums for externally owned values | Possible: add a fallback member (`Unknown`) and map unknown strings to it, so new values don't throw. |
| Skip auxiliary tables | Keep values in configuration or code; `AttributeIdCacheBase` is optional even when using tables. |
| Map with your own attributes or [`JsonStringEnumConverter`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonstringenumconverter) | Fine: [`[Description]`](https://learn.microsoft.com/dotnet/api/system.componentmodel.descriptionattribute) mapping is a helper, not a requirement of any package. |

**What you lose by opting out:** decoupling from upstream value changes and the compact integer lookups provided by auxiliary tables.
