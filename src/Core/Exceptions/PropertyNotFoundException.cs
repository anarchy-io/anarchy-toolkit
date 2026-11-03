namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a property cannot be found on a type through reflection.
/// </summary>
public class PropertyNotFoundException(Type type, string propertyName)
    : NotFoundException($"Property '{propertyName}' not found on type {type.Name}.")
{
    public string PropertyName { get; } = propertyName;
}
