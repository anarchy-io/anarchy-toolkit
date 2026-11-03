using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Web.Helper.Presentation.Contract.Exceptions;

/// <summary>
/// Thrown when a route template cannot be resolved because parameter values are missing.
/// </summary>
public class MissingRouteParameterException : InvalidFormatException
{
    public MissingRouteParameterException(string route, IEnumerable<string> namedParameters)
        : base($"Route '{route}' expects named parameter values for: {string.Join(", ", namedParameters)}.")
    {
        Route = route;
    }

    public MissingRouteParameterException(string route, string parameterName)
        : base($"Route '{route}' expects a value for parameter '{parameterName}'.")
    {
        Route = route;
    }

    public string Route { get; }
}
