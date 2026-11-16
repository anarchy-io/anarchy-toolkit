namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a <see cref="Type"/> argument is expected to be an enum type but is not.
/// </summary>
/// <param name="paramName">The name of the argument holding the type.</param>
/// <param name="type">The offending type; may be <see langword="null"/>.</param>
public class InvalidEnumTypeException(string paramName, Type? type)
    : InvalidArgumentException($"The provided type '{type?.Name ?? "<null>"}' must be an enum type.", paramName)
{
    /// <summary>
    /// Gets the type that is not an enum type.
    /// </summary>
    public Type? Type { get; } = type;
}
