using Newtonsoft.Json;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common.Exceptions;

/// <summary>
/// Thrown when a <see cref="JsonConstructorAttribute"/> parameter does not follow the casing of its matching property.
/// </summary>
/// <remarks>Thrown by <see cref="MultiCasePropertyNamesContractResolver"/>.</remarks>
/// <param name="parameterName">The offending constructor parameter name.</param>
/// <param name="propertyName">The name of the matching property.</param>
public class ConstructorParameterCasingException(string? parameterName, string propertyName)
    : JsonSerializationException(
        $"Constructor parameter '{parameterName}' does not match property '{propertyName}'. Ensure consistent casing.");
