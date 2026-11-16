using Microsoft.AspNetCore.Http;
using System.Net.Mime;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>GET /health/instance</c>: liveness; responds <c>200</c> with an empty body.</item>
/// <item><c>GET /health/services</c>: runs the service health reports; <c>200</c> when all pass, <c>500</c>
/// otherwise, with the JSON report.</item>
/// <item><c>GET /health/status</c>: application and build information as JSON.</item>
/// </list>
/// Non-GET requests to these paths get <c>405</c>; any other path is passed to the next middleware.
/// </remarks>
public abstract class ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddlewareBase"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ApplicationHealthMiddlewareBase(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Handle a HTTP request, serving the health endpoints with <paramref name="healthApi"/> or passing the request
    /// on to the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="healthApi">The health API producing the responses.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    protected async Task InvokeWithHealthApi(HttpContext context, ApplicationHealthApi healthApi)
    {
        if (IsAnyHealthRequest(context) &&
            !HttpMethods.IsGet(context.Request.Method))
        {
            await WriteMethodNotAllowedAsync(context);
        }
        else if (IsInstanceHealthRequest(context))
        {
            return;
        }
        else if (IsApplicationHealthRequest(context))
        {
            var httpResponse = await healthApi.GetHealthHttpResponse(context.RequestAborted);
            await WriteAsJsonAsync(context, httpResponse.Content, (int)httpResponse.StatusCode);
        }
        else if (IsApplicationInformationRequest(context))
        {
            await WriteAsJsonAsync(context, healthApi.GetApplicationInformationAsJson());
        }
        else
        {
            await _next(context);
        }
    }

    private bool IsAnyHealthRequest(HttpContext context) =>
        IsInstanceHealthRequest(context)
        || IsApplicationHealthRequest(context)
        || IsApplicationInformationRequest(context);

    private static bool IsInstanceHealthRequest(HttpContext context) => context.Request.Path == InstanceHealthPath;
    private static bool IsApplicationHealthRequest(HttpContext context) => context.Request.Path == ApplicationHealthPath;
    private static bool IsApplicationInformationRequest(HttpContext context) => context.Request.Path == ApplicationInformationPath;

    private Task WriteMethodNotAllowedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.Response.ContentType = MediaTypeNames.Text.Plain;
        return context.Response.WriteAsync("Only GET requests are allowed.", context.RequestAborted);
    }

    private Task WriteAsJsonAsync(HttpContext context, string content, int statusCode = StatusCodes.Status200OK)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        return context.Response.WriteAsync(content, context.RequestAborted);
    }

    private static readonly PathString InstanceHealthPath = new("/health/instance");
    private static readonly PathString ApplicationHealthPath = new("/health/services");
    private static readonly PathString ApplicationInformationPath = new("/health/status");
    private readonly RequestDelegate _next;
}
