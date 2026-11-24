using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Model;

/// <summary>
/// Provides a static, in-memory, thread-safe cache for mapping string-based attribute
/// values to strongly-typed identifiers, partitioned by a type attribute.
/// </summary>
/// <typeparam name="TAttributeTypes">
/// Enum type that identifies the attribute “dimension” or category (for example,
/// an <c>AuxiliaryTable</c> distinguishing aggregates, events, tasks, etc.).
/// This is used as the first-level key for the cache.
/// </typeparam>
/// <typeparam name="TId">
/// Identifier type used for the cached IDs (for example, <c>int</c>, <see cref="Guid"/>, or <see cref="string"/>).
/// </typeparam>
/// <remarks>
/// <para>
/// This cache is **static per closed generic type**:
/// <c>AttributeIdCacheBase&lt;FooAttribute, int&gt;</c> has its own independent
/// cache, separate from <c>AttributeIdCacheBase&lt;BarAttribute, int&gt;</c>
/// or <c>AttributeIdCacheBase&lt;FooAttribute, long&gt;</c>. All consumers that
/// use the same <typeparamref name="TAttributeTypes"/> and <typeparamref name="TId"/>
/// share the same underlying cache container for the lifetime of the application.
/// </para>
/// <para>
/// The cache is organized as:
/// <c>TAttributeTypes → (string name → TId id)</c>. Within a given
/// <typeparamref name="TAttributeTypes"/> value, the first access can hydrate the entire
/// set of known values from a backing store. Subsequent lookups will use the in-memory
/// dictionary and only fall back to inserting a missing single value when necessary.
/// </para>
/// <para>
/// Typical usage is to create a small, type-safe wrapper class per domain:
/// <code>
/// public enum FooAttribute { TypeAggregate, TypeEvent, TypeTask }
///
/// public sealed class BarMultiAttributeCache : AttributeIdCacheBase&lt;FooAttribute, int&gt;;
/// </code>
/// and then call <see cref="GetOrAddAsync"/> from repositories or services,
/// passing delegates that know how to hydrate and/or insert into the backing store.
/// </para>
/// <para>
/// Safety requirements and limitations:
/// <list type="bullet">
/// <item>
/// This cache has no database identity or transaction outcome. Use it only when every caller of the same closed
/// generic type refers to the same logical dataset and both callbacks return only committed, stable mappings. A
/// callback executing in an open transaction can expose an uncommitted ID to another caller, even if a rollback later
/// clears the cache. Use <see cref="AttributeIdResolver{TAttributeTypes,TId}"/> when that cannot be guaranteed.
/// </item>
/// <item>
/// The cache is **never automatically invalidated or evicted**. You must call
/// <see cref="Clear"/> explicitly (per closed generic type) when the underlying
/// data is changed out-of-band.
/// </item>
/// <item>
/// All subclasses using the same <typeparamref name="TAttributeTypes"/> and
/// <typeparamref name="TId"/> pair share the same cache; use distinct type arguments
/// if you need isolation. Changing the <typeparamref name="TAttributeTypes"/> will be
/// enough to create distinct containers.
/// </item>
/// <item>
/// Concurrent calls may hydrate or insert the same name more than once. The storage operation must atomically
/// get or insert a unique name and all callbacks must return the same committed ID for that name. A concurrent
/// dictionary alone cannot reconcile conflicting IDs returned by the backing store.
/// </item>
/// <item>
/// Because the cache is static and long-lived, avoid caching unbounded or highly
/// volatile datasets, and be aware of the memory footprint over the process lifetime.
/// </item>
/// </list>
/// </para>
/// </remarks>
public abstract class AttributeIdCacheBase<TAttributeTypes, TId>
    where TAttributeTypes : Enum
{
    /// <summary>A single cached row: the identifier of a named attribute value.</summary>
    /// <param name="Id">Identifier of the row in the backing store.</param>
    /// <param name="Name">The attribute value (cache key) the identifier belongs to.</param>
    public record Dto(TId Id, string Name);

    /// <summary>
    /// Loads every known row for <paramref name="attribute"/> from the backing store, used to hydrate an empty
    /// partition of the cache.
    /// </summary>
    /// <param name="attribute">The attribute partition being hydrated.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>All known rows for the attribute.</returns>
    public delegate Task<IReadOnlyCollection<Dto>> HydrateAllAsync(TAttributeTypes attribute,
                                                                   CancellationToken? cancellationToken = null);

    /// <summary>
    /// Inserts (or gets) a single value missing from the cache in the backing store and returns its identifier.
    /// </summary>
    /// <param name="attribute">The attribute partition the value belongs to.</param>
    /// <param name="stringValue">The value to insert.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The identifier of the (possibly pre-existing) row.</returns>
    public delegate Task<TId> InsertMissingAsync(TAttributeTypes attribute, string stringValue,
                                                 CancellationToken? cancellationToken = null);

    private static readonly ConcurrentDictionary<TAttributeTypes, ConcurrentDictionary<string, TId>> Container = new();

    /// <summary>
    /// Clears all cached identifiers for this specific closed generic type,
    /// forcing a full re-hydration on the next access for any attribute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This affects only the cache of the current closed generic type
    /// <c>AttributeIdCacheBase&lt;TAttributeTypes, TId&gt;</c>. Other generic
    /// instantiations (for example, different attribute enums or ID types) are not
    /// impacted.
    /// </para>
    /// <para>
    /// Use this when:
    /// <list type="bullet">
    /// <item>Bulk changes have been applied to the underlying store.</item>
    /// <item>You want to ensure the next call to <see cref="GetOrAddAsync"/> performs
    /// a fresh hydration rather than relying on stale in-memory data.</item>
    /// </list>
    /// </para>
    /// <para>
    /// An in-flight lookup may still return a value obtained before the clear. Coordinate callers with out-of-band
    /// changes if they must not observe stale values. Clearing also increases backing-store load as lookups rehydrate.
    /// </para>
    /// </remarks>
    public static void Clear() => Container.Clear();

    /// <summary>
    /// Gets the identifier for the given attribute and string value from the cache
    /// if present, or hydrates/inserts it via the provided delegates and caches the result.
    /// </summary>
    /// <param name="typeAttribute">
    /// The attribute “dimension” (for example, an <c>FooAttribute</c> value) that
    /// partitions the cache. Each <paramref name="typeAttribute"/> value has its own
    /// inner name → ID dictionary.
    /// </param>
    /// <param name="attributeKey">
    /// The string value whose identifier should be resolved (for example, a type name
    /// or domain-specific key).
    /// </param>
    /// <param name="callbackHydrateAll">
    /// Delegate used to hydrate **all** known rows for the given
    /// <paramref name="typeAttribute"/> when its cache is still empty. Concurrent calls may invoke it more than once;
    /// an empty result also permits later hydration. The returned collection is added into the cache before lookup.
    /// </param>
    /// <param name="callbackInsertMissing">
    /// Delegate used to insert a **single** missing value for the given
    /// <paramref name="typeAttribute"/> and <paramref name="attributeKey"/> into the
    /// backing store when it cannot be found in the cache (even after hydration).
    /// The returned identifier is then stored in the cache and returned to the caller.
    /// </param>
    /// <param name="cancellationToken">Token forwarded to both callbacks to cancel the operation.</param>
    /// <returns>
    /// The resolved identifier for <paramref name="attributeKey"/> under the given
    /// <paramref name="typeAttribute"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Resolution flow:
    /// <list type="number">
    /// <item>Check the in-memory cache for <paramref name="typeAttribute"/> /
    /// <paramref name="attributeKey"/>; if found, return immediately.</item>
    /// <item>
    /// If the cache for <paramref name="typeAttribute"/> is empty, invoke
    /// <paramref name="callbackHydrateAll"/>, add all returned rows to the cache, and
    /// re-check for <paramref name="attributeKey"/>.
    /// </item>
    /// <item>
    /// If still not found, invoke <paramref name="callbackInsertMissing"/> to create a single
    /// entry in the backing store, add the resulting ID to the cache, and return it.
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// **Usage pattern:**
    /// <code>
    /// var id = await BarMultiAttributeCache.GetOrAddAsync(
    ///     FooAttribute.TypeA,
    ///     attributeName,
    ///     callbackHydrateAll: (attribute, ct) => fooBarAttributeRepository.GetAllAsync(cancellationToken: ct),
    ///     callbackInsertMissing: (attribute, name, ct) => fooBarAttributeRepository.GetOrInsert(attribute, name, ct),
    ///     cancellationToken);
    /// </code>
    /// </para>
    /// <para>
    /// **Things to watch out for:**
    /// <list type="bullet">
    /// <item>
    /// <paramref name="callbackHydrateAll"/> is only called when the inner cache for a given
    /// <paramref name="typeAttribute"/> is completely empty. If you need to observe
    /// incremental changes, you must either manage those via <paramref name="callbackInsertMissing"/>
    /// or call <see cref="Clear"/> when appropriate.
    /// </item>
    /// <item>
    /// Multiple concurrent calls for the same <paramref name="typeAttribute"/> and
    /// <paramref name="attributeKey"/> may race: it is possible for
    /// <paramref name="callbackHydrateAll"/> or <paramref name="callbackInsertMissing"/> to be invoked
    /// more than once under contention. Enforce uniqueness and use an atomic get-or-insert in storage; all callbacks
    /// must return the same committed mapping.
    /// </item>
    /// <item>
    /// Because the cache never evicts entries automatically, avoid using unbounded
    /// free-form strings as keys without some external lifecycle management.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static async Task<TId> GetOrAddAsync(
        TAttributeTypes typeAttribute,
        string attributeKey,
        HydrateAllAsync callbackHydrateAll,
        InsertMissingAsync callbackInsertMissing,
        CancellationToken? cancellationToken = null)
    {
        var typeCache = Container.GetOrAdd(typeAttribute, _ => new ConcurrentDictionary<string, TId>());

        if (typeCache.TryGetValue(attributeKey, out var cachedId))
        {
            return cachedId;
        }

        if (typeCache.IsEmpty)
        {
            var rows = await callbackHydrateAll(typeAttribute, cancellationToken);

            foreach (var row in rows)
            {
                typeCache.TryAdd(row.Name, row.Id);
            }

            if (typeCache.TryGetValue(attributeKey, out cachedId))
            {
                return cachedId;
            }
        }

        var newId = await callbackInsertMissing(typeAttribute, attributeKey, cancellationToken);
        typeCache[attributeKey] = newId;

        return newId;
    }
}
