namespace Anarchy.Toolkit.Core.Extension;

/// <summary>
/// Extension methods for dictionaries and nested collections.
/// </summary>
public static class CollectionExtension
{
    /// <summary>
    /// Copies every entry of <paramref name="additional"/> into this dictionary, overwriting existing keys.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="this">The dictionary to mutate.</param>
    /// <param name="additional">The entries to add or overwrite.</param>
    /// <returns>The same (mutated) instance passed as <paramref name="this"/>, for chaining.</returns>
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        this Dictionary<TKey, TValue> @this,
        Dictionary<TKey, TValue> additional) where TKey : notnull
    {
        foreach (var kvp in additional)
        {
            @this[kvp.Key] = kvp.Value;
        }

        return @this;
    }

    /// <summary>
    /// Copies every entry of <paramref name="additional"/> into this dictionary, overwriting existing keys.
    /// </summary>
    /// <remarks>
    /// Both arguments are cast to <see cref="Dictionary{TKey, TValue}"/>; other <see cref="IDictionary{TKey, TValue}"/>
    /// implementations are not supported.
    /// </remarks>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="this">The dictionary to mutate; must be a <see cref="Dictionary{TKey, TValue}"/>.</param>
    /// <param name="additional">The entries to add or overwrite; must be a <see cref="Dictionary{TKey, TValue}"/>.</param>
    /// <returns>The same (mutated) instance passed as <paramref name="this"/>.</returns>
    /// <exception cref="InvalidCastException">Either argument is not a <see cref="Dictionary{TKey, TValue}"/>.</exception>
    public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> @this,
        IDictionary<TKey, TValue> additional) where TKey : notnull
    {
        return ((Dictionary<TKey, TValue>)@this).Merge((Dictionary<TKey, TValue>)additional);
    }

    /// <summary>
    /// Merges a collection of dictionaries into one, concatenating the value collections of keys that appear in
    /// more than one dictionary.
    /// </summary>
    /// <typeparam name="T">The element type of the value collections.</typeparam>
    /// <param name="dictionaries">The dictionaries to merge.</param>
    /// <returns>A new dictionary with one entry per distinct key.</returns>
    public static Dictionary<string, IReadOnlyCollection<T>> Flatten<T>(
        this IReadOnlyCollection<Dictionary<string, IReadOnlyCollection<T>>> dictionaries)
    {
        return dictionaries
            .SelectMany(dict => dict)
            .GroupBy(pair => pair.Key)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<T>)group
                    .SelectMany(pair => pair.Value)
                    .ToList()
            );
    }

    /// <summary>
    /// Flattens a collection of collections into a single list.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The nested collection.</param>
    /// <returns>A new, materialized list with all elements in order.</returns>
    public static IReadOnlyCollection<T> Flatten<T>(
        this IReadOnlyCollection<IReadOnlyCollection<T>> collection)
    {
        return collection.SelectMany(x => x).ToList();
    }

    /// <summary>
    /// Flattens a sequence of sequences into a single, eagerly materialized sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The nested sequence.</param>
    /// <returns>A new list (exposed as <see cref="IEnumerable{T}"/>) with all elements in order.</returns>
    public static IEnumerable<T> Flatten<T>(
        this IEnumerable<IEnumerable<T>> collection)
    {
        return collection.SelectMany(x => x).ToList();
    }
}
