using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Helper;

/// <summary>
/// Array utilities parameterised by element type.
/// </summary>
/// <typeparam name="T">The array element type.</typeparam>
public static class ArrayHelper<T>
{
    /// <summary>
    /// Creates a copy of <paramref name="array"/>, deep-cloning elements when possible.
    /// </summary>
    /// <param name="array">The array to copy; may be <see langword="null"/>.</param>
    /// <returns>
    ///     Returns an empty array in case <paramref name="array" /> is null. Otherwise, returns a new array with
    ///     elements that are copied from <paramref name="array" />.
    /// </returns>
    /// <remarks>
    ///     Note that if <typeparamref name="T" /> does not implement <see cref="IDeepCloneableEntity{T}" />,
    ///     the elements of the returned collection will be copied by reference instead.
    /// </remarks>
    public static T[] DeepClone(T[]? array)
    {
        if (array == null || array.Length == 0)
        {
            return [];
        }

        var genericType = typeof(T);
        var containsDeepCloneType = genericType.GetInterfaces()
            .Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IDeepCloneableEntity<>));

        return containsDeepCloneType ?
            (from IDeepCloneableEntity<T> item in array select item.DeepClone()).ToArray() :
            (from item in array select item).ToArray();
    }
}
