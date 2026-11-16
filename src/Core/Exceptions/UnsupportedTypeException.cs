namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a code path receives an instance of a type it does not know how to handle.
/// </summary>
/// <param name="type">The unsupported type.</param>
public class UnsupportedTypeException(Type type) : UnsupportedOperationException($"Unsupported type {type}.")
{
    /// <summary>
    /// Gets the unsupported type.
    /// </summary>
    public Type Type { get; } = type;
}
