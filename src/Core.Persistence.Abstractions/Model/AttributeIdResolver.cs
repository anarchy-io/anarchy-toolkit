namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Model;

/// <summary>
/// Resolves attribute identifiers against the caller's storage without sharing results between calls.
/// </summary>
/// <typeparam name="TAttributeTypes">Enum identifying the attribute partition.</typeparam>
/// <typeparam name="TId">Identifier type, such as a number, string, or <see cref="Guid"/>.</typeparam>
/// <remarks>
/// The callbacks expose neither database identity nor transaction outcomes. Consequently this resolver retains no
/// identifiers, hydrated or inserted, between calls; it cannot publish an uncommitted ID to another caller. The
/// returned ID can still be uncommitted and must remain within the caller's transaction until that transaction commits.
/// Both callbacks must use the caller's intended database and transaction. Concurrent registration of a name requires
/// a storage-enforced uniqueness constraint and an atomic get-or-insert operation; this resolver does not lock or
/// coordinate transactions. Unlike <see cref="AttributeIdCacheBase{TAttributeTypes,TId}"/>, each call hydrates anew.
/// </remarks>
/// <example>
/// <code>
/// // Both repository operations participate in the current caller-owned transaction.
/// var id = await AttributeIdResolver&lt;MyAttribute, Guid&gt;.GetOrAddAsync(
///     attribute, name,
///     (kind, ct) =&gt; repository.GetAllAsync(kind, ct),
///     (kind, value, ct) =&gt; repository.GetOrInsertAtomicallyAsync(kind, value, ct),
///     cancellationToken);
/// // Use id within that transaction; do not share it before the owner commits.
/// </code>
/// </example>
public static class AttributeIdResolver<TAttributeTypes, TId> where TAttributeTypes : Enum
{
    /// <summary>Finds or inserts an identifier visible to the caller, without caching it.</summary>
    /// <param name="typeAttribute">The attribute partition to query.</param>
    /// <param name="attributeKey">The name to resolve, compared using ordinal string equality.</param>
    /// <param name="callbackHydrateAll">Loads rows visible to the caller's database and transaction.</param>
    /// <param name="callbackInsertMissing">Atomically gets or inserts a missing name in the caller's storage.</param>
    /// <param name="cancellationToken">Token forwarded to the callbacks.</param>
    /// <returns>The identifier visible to the caller, which may not yet be committed.</returns>
    public static async Task<TId> GetOrAddAsync(
        TAttributeTypes typeAttribute,
        string attributeKey,
        AttributeIdCacheBase<TAttributeTypes, TId>.HydrateAllAsync callbackHydrateAll,
        AttributeIdCacheBase<TAttributeTypes, TId>.InsertMissingAsync callbackInsertMissing,
        CancellationToken? cancellationToken = null)
    {
        var rows = await callbackHydrateAll(typeAttribute, cancellationToken);
        foreach (var row in rows)
        {
            if (string.Equals(row.Name, attributeKey, StringComparison.Ordinal))
            {
                return row.Id;
            }
        }

        return await callbackInsertMissing(typeAttribute, attributeKey, cancellationToken);
    }
}
