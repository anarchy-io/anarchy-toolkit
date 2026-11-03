namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a <see cref="Type"/> argument is expected to be an enum type but is not.
/// </summary>
public class InvalidEnumTypeException(string paramName, Type? type)
    : InvalidArgumentException($"The provided type '{type?.Name ?? "<null>"}' must be an enum type.", paramName)
{
    public Type? Type { get; } = type;
}
