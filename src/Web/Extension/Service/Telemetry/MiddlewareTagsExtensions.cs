namespace Anarchy.Toolkit.Web.Extension.Service.Telemetry;

/// <summary>
/// HTTP-specific helpers for low-cardinality metric tags recorded by web middleware.
/// </summary>
/// <remarks>
/// Used by <see cref="Anarchy.Toolkit.Web.Service.Host.Middleware.LoggingMiddleware"/> to tag the request duration metric.
/// Controller and action names are read from the route pattern defaults of the matched <see cref="RouteEndpoint"/>;
/// when no route endpoint is matched the builder is returned unchanged.
/// </remarks>
public static class MiddlewareTagsExtensions
{
    /// <summary>
    /// Creates tags for an HTTP response: a success flag based on the status code and the textual status code.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP response status code.</param>
    /// <returns>The initial metric tags.</returns>
    public static KeyValuePair<string, object?>[] FromStatusCode(int httpStatusCode) =>
        [new("success", httpStatusCode is >= 200 and < 300), new("status_code", httpStatusCode.ToString())];

    /// <summary>Adds the <c>controller</c> tag from the matched endpoint's route defaults, when available.</summary>
    /// <param name="tags">The current metric tags.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>The tags with a controller name when one is available.</returns>
    public static KeyValuePair<string, object?>[] WithController(this KeyValuePair<string, object?>[] tags, HttpContext context)
    {
        var routeValues = ExtractRouteValues(context);
        return routeValues is not null && routeValues.TryGetValue("controller", out var controllerName)
            ? [.. tags, new("controller", controllerName?.ToString() ?? "")]
            : tags;
    }

    /// <summary>Adds the <c>controller_action</c> tag from the matched endpoint's route defaults, when available.</summary>
    /// <param name="tags">The current metric tags.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>The tags with an action name when one is available.</returns>
    public static KeyValuePair<string, object?>[] WithAction(this KeyValuePair<string, object?>[] tags, HttpContext context)
    {
        var routeValues = ExtractRouteValues(context);
        return routeValues is not null && routeValues.TryGetValue("action", out var actionName)
            ? [.. tags, new("controller_action", actionName?.ToString() ?? "")]
            : tags;
    }

    /// <summary>Adds the <c>request_method</c> tag (e.g. <c>GET</c>).</summary>
    /// <param name="tags">The current metric tags.</param>
    /// <param name="httpRequest">The current HTTP request.</param>
    /// <returns>The tags with the request method.</returns>
    public static KeyValuePair<string, object?>[] WithRequestMethod(this KeyValuePair<string, object?>[] tags, HttpRequest httpRequest) =>
        [.. tags, new("request_method", httpRequest.Method)];

    /// <summary>Adds the <c>request_path</c> tag using the request's <see cref="HttpRequest.PathBase"/>.</summary>
    /// <remarks>Beware of metric cardinality when tagging paths.</remarks>
    /// <param name="tags">The current metric tags.</param>
    /// <param name="httpRequest">The current HTTP request.</param>
    /// <returns>The tags with the request path.</returns>
    public static KeyValuePair<string, object?>[] WithRequestPath(this KeyValuePair<string, object?>[] tags, HttpRequest httpRequest) =>
        [.. tags, new("request_path", httpRequest.PathBase.Value ?? "")];

    private static IReadOnlyDictionary<string, object?>? ExtractRouteValues(HttpContext context) =>
        (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.Defaults;
}
