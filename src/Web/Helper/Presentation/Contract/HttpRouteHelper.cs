using Anarchy.Toolkit.Web.Helper.Presentation.Contract.Exceptions;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Web.Helper.Presentation.Contract;

/// <summary>
/// Builds concrete URLs from ASP.NET Core style route templates (e.g. <c>orders/{id:int}</c>).
/// </summary>
public static partial class HttpRouteHelper
{
    [GeneratedRegex(@"\{(?<name>[^}:]+)(:[^}]+)?\}")]
    private static partial Regex RouteParameterRegex();

    /// <summary>
    /// Combines <paramref name="routeRelative"/> with <paramref name="routeBase"/> and replaces the template
    /// parameters with the supplied values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <paramref name="routeRelative"/> starting with <c>/</c> is used as is; an empty one yields the base route.
    /// If the route has no <c>{name}</c> parameters, <paramref name="routeParameters"/> are applied with
    /// <see cref="string.Format(IFormatProvider, string, object[])"/>.
    /// </para>
    /// <para>
    /// A single non-scalar argument (dictionary or object with public properties) is treated as named values,
    /// matched case-insensitively. Otherwise values are consumed positionally. Values are formatted with the
    /// invariant culture; route constraints (e.g. <c>:int</c>) are dropped.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// HttpRouteHelper.FromTemplate("{id:int}/items", "orders", 42);          // "orders/42/items"
    /// HttpRouteHelper.FromTemplate("{id}/items/{itemId}", "orders", new { id = 1, itemId = 7 }); // "orders/1/items/7"
    /// </code>
    /// </example>
    /// <param name="routeRelative">The route template, relative to <paramref name="routeBase"/> unless it starts with <c>/</c>.</param>
    /// <param name="routeBase">The optional base route (e.g. a controller route).</param>
    /// <param name="routeParameters">Positional values, or a single object/dictionary of named values.</param>
    /// <returns>The resolved route.</returns>
    /// <exception cref="MissingRouteParameterException">
    /// A named-values object contains none of the template parameters, or there are fewer positional values than parameters.
    /// </exception>
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
