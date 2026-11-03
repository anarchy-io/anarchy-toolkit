using System.Collections.Concurrent;
using System.Numerics;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Model;

/// <summary>
/// Provides a static, in-memory, thread-safe cache for mapping string-based attribute
/// values to strongly-typed numeric identifiers, partitioned by a type attribute.
/// </summary>
/// <typeparam name="TAttributeTypes">
/// Enum type that identifies the attribute “dimension” or category (for example,
/// an <c>AuxiliaryTable</c> distinguishing aggregates, events, tasks, etc.).
/// This is used as the first-level key for the cache.
/// </typeparam>
/// <typeparam name="TId">
/// Numeric identifier type used for the cached IDs (for example, <c>int</c> or
/// <c>long</c>). Must implement <see cref="System.Numerics.IBinaryNumber{TId}"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// This cache is **static per closed generic type**:
/// <c>IdentifiableMultiAttributeCache&lt;FooAttribute, int&gt;</c> has its own independent
/// cache, separate from <c>IdentifiableMultiAttributeCache&lt;BarAttribute, int&gt;</c>
/// or <c>IdentifiableMultiAttributeCache&lt;FooAttribute, long&gt;</c>. All consumers that
/// use the same <typeparamref name="TAttributeTypes"/> and <typeparamref name="TId"/>
/// share the same underlying cache container for the lifetime of the application.
/// </para>
/// <para>
/// The cache is organized as:
/// <c>TTypeAttribute → (string name → TId id)</c>. Within a given
/// <typeparamref name="TAttributeTypes"/> value, the first access can hydrate the entire
/// set of known values from a backing store. Subsequent lookups will use the in-memory
/// dictionary and only fall back to inserting a missing single value when necessary.
/// </para>
/// <para>
/// Typical usage is to create a small, type-safe wrapper class per domain:
/// <code>
/// public enum FooAttribute { TypeAggregate, TypeEvent, TypeTask }
///
/// public sealed class BarMultiAttributeCache : IdentifiableMultiAttributeCache&lt;FooAttribute, int&gt;;
/// </code>
/// and then call <see cref="GetOrAddAsync"/> from repositories or services,
/// passing delegates that know how to hydrate and/or insert into the backing store.
/// </para>
/// <para>
/// **Caveats &amp; things to watch out for:**
/// <list type="bullet">
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
/// Under contention, multiple concurrent calls may trigger hydration or insertion to
/// work in parallel, but the internal <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
/// ensures the final cached mapping is consistent.
/// </item>
/// <item>
/// Because the cache is static and long-lived, avoid caching unbounded or highly
/// volatile datasets, and be aware of the memory footprint over the process lifetime.
/// </item>
/// </list>
/// </para>
/// </remarks>
public abstract class AttributeIdCacheBase<TAttributeTypes, TId>
    where TAttributeTypes : Enum where TId : IBinaryNumber<TId>
{
    public record Dto(TId Id, string Name);

    public delegate Task<IReadOnlyCollection<Dto>> HydrateAllAsync(TAttributeTypes attribute,
                                                                   CancellationToken? cancellationToken = null);

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
    /// <c>IdentifiableMultiAttributeCache&lt;TTypeAttribute, TId&gt;</c>. Other generic
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
    /// Be aware that calling <see cref="Clear"/> in a hot path can briefly increase
    /// load on the backing store, as the next lookups will need to re-hydrate.
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
    /// <paramref name="typeAttribute"/> when its cache is still empty. It is called at
    /// most once per attribute value per process lifetime, unless
    /// <see cref="Clear"/> is invoked. The returned collection is bulk-added into the
    /// cache before attempting a lookup again.
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
    /// <paramref name="callbackHydrateAll"/> once, add all returned rows to the cache, and
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
    ///     hydrateAll: (attribute, ct) => fooBarAttributeRepository.GetAllAsync(cancellationToken: ct),
    ///     insertSingle: (attribute, name, ct) => fooBarAttributeRepository.GetOrInsert(attribute, name, ct),
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
    /// more than once under heavy contention. Implement these delegates to be idempotent
    /// or otherwise safe for such scenarios.
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
