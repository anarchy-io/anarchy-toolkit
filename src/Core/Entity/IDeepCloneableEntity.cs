namespace Anarchy.Toolkit.Core.Entity;

/// <summary>
/// Contract for entities that can produce a deep copy of themselves.
/// </summary>
/// <remarks>
/// <see cref="Helpers.Arrays.ArrayHelper{T}.DeepClone(T[])"/> uses this interface to deep-copy array elements.
/// </remarks>
/// <typeparam name="T">The type of the produced copy (usually the implementing type itself).</typeparam>
public interface IDeepCloneableEntity<out T>
{
    /// <summary>
    /// Creates a deep copy of this instance, sharing no mutable state with the original.
    /// </summary>
    /// <returns>A new, independent copy.</returns>
    T DeepClone();
}
