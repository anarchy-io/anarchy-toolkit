using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Web.Extension.Service.Observability.TagBuilderBase;
using System.Diagnostics;
using CbTagBuilder = Anarchy.Toolkit.Core.Observability.Service.Observability.TagBuilderBase;

namespace Anarchy.Toolkit.Web.Service.Host.Middleware;

public class LoggingMiddleware(
    RequestDelegate next,
    Observer observer)
{
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
