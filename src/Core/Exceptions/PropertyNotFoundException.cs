namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a property cannot be found on a type through reflection.
/// </summary>
/// <param name="type">The type that was inspected.</param>
/// <param name="propertyName">The name of the missing property.</param>
public class PropertyNotFoundException(Type type, string propertyName)
    : NotFoundException($"Property '{propertyName}' not found on type {type.Name}.")
{
    /// <summary>
    /// Gets the name of the property that could not be found.
    /// </summary>
    public string PropertyName { get; } = propertyName;
}
