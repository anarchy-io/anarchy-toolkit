using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Runtime.Web.Helpers.Presentation.Contract.Exceptions;

/// <summary>
/// Thrown when a route template cannot be resolved because parameter values are missing.
/// </summary>
public class MissingRouteParameterException : InvalidFormatException
{
    /// <summary>Creates the exception for a route whose named parameters were not supplied.</summary>
    /// <param name="route">The resolved route template.</param>
    /// <param name="namedParameters">The parameter names expected by the template.</param>
    public MissingRouteParameterException(string route, IEnumerable<string> namedParameters)
        : base($"Route '{route}' expects named parameter values for: {string.Join(", ", namedParameters)}.")
    {
        Route = route;
    }

    /// <summary>Creates the exception for a route missing a value for a single parameter.</summary>
    /// <param name="route">The resolved route template.</param>
    /// <param name="parameterName">The parameter with no value.</param>
    public MissingRouteParameterException(string route, string parameterName)
        : base($"Route '{route}' expects a value for parameter '{parameterName}'.")
    {
        Route = route;
    }

    /// <summary>The route template that could not be resolved.</summary>
    public string Route { get; }
}
