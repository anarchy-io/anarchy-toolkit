namespace Anarchy.Toolkit.Core.Extension;

public static class CollectionExtension
{
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

    public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> @this,
        IDictionary<TKey, TValue> additional) where TKey : notnull
    {
        return ((Dictionary<TKey, TValue>)@this).Merge((Dictionary<TKey, TValue>)additional);
    }

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

    public static IReadOnlyCollection<T> Flatten<T>(
        this IReadOnlyCollection<IReadOnlyCollection<T>> collection)
    {
        return collection.SelectMany(x => x).ToList();
    }

    public static IEnumerable<T> Flatten<T>(
        this IEnumerable<IEnumerable<T>> collection)
    {
        return collection.SelectMany(x => x).ToList();
    }
}
