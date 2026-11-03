using Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

public class MultiCasePropertyNamesContractResolver : DefaultContractResolver
{
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
