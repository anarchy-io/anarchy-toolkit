using Anarchy.Toolkit.Web.Helper.Presentation.Contract.Exceptions;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Web.Helper.Presentation.Contract;

public static partial class HttpRouteHelper
{
    [GeneratedRegex(@"\{(?<name>[^}:]+)(:[^}]+)?\}")]
    private static partial Regex RouteParameterRegex();

    public static string FromTemplate(string routeRelative, string? routeBase = null, params object[] routeParameters)
    {
        var resolvedRoute = routeRelative.StartsWith("/", StringComparison.Ordinal) ?
            routeRelative :
            string.IsNullOrEmpty(routeRelative) ?
                routeBase?.TrimEnd('/') ?? string.Empty :
                $"{routeBase?.TrimEnd('/')}/{routeRelative.TrimStart('/')}";

        var templateMatches = RouteParameterRegex().Matches(resolvedRoute);
        if (templateMatches.Count == 0)
            return routeParameters.Length == 0 ?
                resolvedRoute :
                string.Format(CultureInfo.InvariantCulture, resolvedRoute, routeParameters);

        var templateParameterNames = templateMatches
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var remainingPositionalParameters = routeParameters;
        var namedParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (routeParameters.Length == 1 &&
            TryGetNamedRouteParameters(routeParameters[0], out var extractedNamedParameters))
        {
            var hasAnyTemplateNamedValue = extractedNamedParameters.Keys
                .Any(templateParameterNames.Contains);

            if (hasAnyTemplateNamedValue)
            {
                namedParameters = extractedNamedParameters;
                remainingPositionalParameters = Array.Empty<object>();
            }
            else
            {
                throw new MissingRouteParameterException(resolvedRoute, templateParameterNames);
            }
        }

        var positionalIndex = 0;
        return RouteParameterRegex().Replace(resolvedRoute, match => {
            var parameterName = match.Groups["name"].Value;

            if (namedParameters.TryGetValue(parameterName, out var namedValue))
                return Convert.ToString(namedValue, CultureInfo.InvariantCulture) ?? string.Empty;

            if (positionalIndex >= remainingPositionalParameters.Length)
                throw new MissingRouteParameterException(resolvedRoute, parameterName);

            var positionalValue = remainingPositionalParameters[positionalIndex++];
            return Convert.ToString(positionalValue, CultureInfo.InvariantCulture) ?? string.Empty;
        });
    }

    private static bool TryGetNamedRouteParameters(object? routeParameter, out Dictionary<string, object?> values)
    {
        values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        switch (routeParameter)
        {
            case null:
            case string or Guid or DateTime or DateTimeOffset or decimal:
                return false;
        }

        if (routeParameter.GetType().IsPrimitive || routeParameter.GetType().IsEnum)
            return false;

        switch (routeParameter)
        {
            case IReadOnlyDictionary<string, object?> readOnlyDictionary:
                {
                    foreach (var (key, value) in readOnlyDictionary)
                        values[key] = value;

                    return true;
                }
            case IDictionary<string, object?> dictionary:
                {
                    foreach (var (key, value) in dictionary)
                        values[key] = value;

                    return true;
                }
        }

        var publicProperties = routeParameter.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead)
            .ToArray();

        if (publicProperties.Length == 0)
            return false;

        foreach (var property in publicProperties)
            values[property.Name] = property.GetValue(routeParameter);

        return true;
    }
}
