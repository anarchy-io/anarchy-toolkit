using Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

/// <summary>
/// Newtonsoft.Json contract resolver that validates that <see cref="JsonConstructorAttribute"/> parameters follow the
/// naming strategy (camelCase by default) of their matching properties.
/// </summary>
/// <remarks>
/// Guards against silent deserialization bugs where a constructor parameter does not bind because its casing differs
/// from the expected property name. When <see cref="DefaultContractResolver.NamingStrategy"/> is not set,
/// <see cref="CamelCaseNamingStrategy"/> is assumed.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var settings = new JsonSerializerSettings { ContractResolver = new MultiCasePropertyNamesContractResolver() };
/// var model = await response.ReadContentAsync&lt;OrderApiModel&gt;(settings);
/// </code>
/// </example>
public class MultiCasePropertyNamesContractResolver : DefaultContractResolver
{
    /// <inheritdoc/>
    /// <exception cref="ConstructorParameterCasingException">
    /// A <see cref="JsonConstructorAttribute"/> constructor parameter matches a property case-insensitively but not
    /// under the naming strategy.
    /// </exception>
    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);

        // Check if the type has a constructor marked with [JsonConstructor]
        var constructors = objectType.GetConstructors();
        var jsonConstructor = constructors.FirstOrDefault(c =>
            c.GetCustomAttributes(typeof(JsonConstructorAttribute), true).Any());

        if (jsonConstructor == null)
        {
            return contract;
        }

        // Get the naming strategy from the CamelCasePropertyNamesContractResolver
        var namingStrategy = NamingStrategy ?? new CamelCaseNamingStrategy();

        foreach (var parameter in jsonConstructor.GetParameters())
        {
            // Check if there's a matching property in PascalCase, adjust the parameter name to camelCase
            var matchingProperty = objectType.GetProperty(parameter.Name ?? string.Empty,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (matchingProperty != null &&
                parameter.Name != namingStrategy.GetPropertyName(matchingProperty.Name, false))
            {
                throw new ConstructorParameterCasingException(parameter.Name, matchingProperty.Name);
            }
        }

        return contract;
    }
}
