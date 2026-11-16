using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Extension.Service.Observability.TagBuilderBase;
using System.Diagnostics;
using CbTagBuilder = Anarchy.Toolkit.Core.Observability.Service.Observability.TagBuilderBase;

namespace Anarchy.Toolkit.Web.Service.Host.Middleware;

/// <summary>
/// Middleware that logs each completed request (method, path and status code) and records its duration metric.
/// </summary>
/// <remarks>
/// The duration is tagged with the status code, controller, action and request method (see
/// <see cref="Anarchy.Toolkit.Web.Extension.Service.Observability.TagBuilderBase.MiddlewareTagsExtensions"/>). Nothing is
/// logged if a downstream component throws. Registered by <c>UseToolkitHostRestApi</c> before
/// <see cref="ExceptionMiddleware"/>.
/// </remarks>
/// <param name="next">The next delegate in the pipeline.</param>
/// <param name="observer">The observer used for logging and metrics.</param>
public class LoggingMiddleware(
    RequestDelegate next,
    Observer observer)
{
    /// <summary>Invokes the rest of the pipeline, then logs the request and records its duration.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that completes when the request has been processed.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var caller = this.CalledFrom();
        var timer = Stopwatch.StartNew();

        // Execute all next steps in the pipeline
        await next(context);

        var statusCode = context.Response.StatusCode;
        var request = context.Request;

        observer.LogInformation(
            caller,
            "Finished {Method} request for route {Path} with code {StatusCode}",
            request.Method,
            request.Path,
            statusCode);

        observer.ExecutionRecordDuration(caller, timer.Elapsed, CbTagBuilder.FromStatusCode(statusCode)
            .WithController(context)
            .WithAction(context)
            .WithRequestMethod(request)
            // .WithRequestPath(request)
            );
    }
}
