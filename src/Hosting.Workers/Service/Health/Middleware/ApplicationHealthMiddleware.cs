using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status).
/// </summary>
public class ApplicationHealthMiddleware : ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddleware"/>.
    /// </summary>
    public ApplicationHealthMiddleware(RequestDelegate next) : base(next) { }

    /// <summary>
    /// Handle a HTTP request.
    /// </summary>
    public Task Invoke(HttpContext context, ApplicationHealthApi healthApi) => InvokeWithHealthApi(context, healthApi);
}
