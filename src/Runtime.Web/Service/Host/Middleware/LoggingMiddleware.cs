using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Runtime.Web.Extensions.System.Collections.Generic;
using System.Diagnostics;

namespace Anarchy.Toolkit.Runtime.Web.Service.Host.Middleware;

/// <summary>
/// Middleware that logs each completed request (method, path and status code) and records its duration metric.
/// </summary>
/// <remarks>
/// The duration is tagged with the status code, controller, action and request method (see
/// <see cref="MiddlewareTagsExtensions"/>). Nothing is
/// logged if a downstream component throws. Registered by <c>UseToolkitHostRestApi</c> before
/// <see cref="ExceptionMiddleware"/>.
/// </remarks>
/// <param name="next">The next delegate in the pipeline.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public class LoggingMiddleware(
    RequestDelegate next,
    AppTelemetry telemetry)
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

        telemetry.Log(
            LogLevel.Information,
            caller,
            null,
            "Finished {Method} request for route {Path} with code {StatusCode}",
            request.Method,
            request.Path,
            statusCode);

        telemetry.RecordDuration(caller, timer.Elapsed, MiddlewareTagsExtensions.FromStatusCode(statusCode)
            .WithController(context)
            .WithAction(context)
            .WithRequestMethod(request)
            // .WithRequestPath(request)
            );
    }
}
