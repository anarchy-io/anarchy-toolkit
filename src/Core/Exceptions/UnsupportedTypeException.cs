namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a code path receives an instance of a type it does not know how to handle.
/// </summary>
public class UnsupportedTypeException(Type type) : UnsupportedOperationException($"Unsupported type {type}.")
{
    public Type Type { get; } = type;
}
