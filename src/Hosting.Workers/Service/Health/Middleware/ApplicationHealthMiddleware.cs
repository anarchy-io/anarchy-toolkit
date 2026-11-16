using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status).
/// The <see cref="ApplicationHealthApi"/> is resolved from the request services on each invocation, so it must be
/// registered in DI (e.g. with <c>AddApplicationHealthEndpoints</c>).
/// </summary>
/// <remarks>See <see cref="ApplicationHealthMiddlewareBase"/> for the endpoint behavior.</remarks>
public class ApplicationHealthMiddleware : ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ApplicationHealthMiddleware(RequestDelegate next) : base(next) { }

    /// <summary>
    /// Handle a HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="healthApi">The health API, injected from the request services.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    public Task Invoke(HttpContext context, ApplicationHealthApi healthApi) => InvokeWithHealthApi(context, healthApi);
}
