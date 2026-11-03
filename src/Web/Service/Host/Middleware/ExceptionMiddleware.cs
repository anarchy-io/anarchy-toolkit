using Anarchy.Toolkit.Core.Health.Entity.Api.Output;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Web.Presentation.Attribute;
using Microsoft.AspNetCore.Mvc.Controllers;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Web.Service.Host.Middleware;

/// <summary>
///     Middleware responsible for handling Exceptions being thrown and converting them to the correct StatusCode.
///     Also, while handling any exceptions the ErrorStack, ErrorMessage and BaseError are being extracted and written
///     to the Response Body.
/// </summary>
public class ExceptionMiddleware(
    RequestDelegate next,
    Observer observability)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsIgnoreExceptionHandlingAttributeApplied(context))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception, context);
        }
    }

    private async Task HandleExceptionAsync(Exception exception, HttpContext httpContext)
    {
        var caller = this.CalledFrom();
        var baseException = exception is UnitOfWorkSessionException ? exception.GetBaseException() : exception;

        var statusCode = baseException switch {
            InvalidOperationException or JsonException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;
        observability.LogError(exception, caller, "Request failed with an unhandled exception in middleware");

        await httpContext.Response.WriteAsJsonAsync(new ExceptionReport(exception));
    }

    private static bool IsIgnoreExceptionHandlingAttributeApplied(HttpContext context)
    {
        var methodInfo = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>()?.MethodInfo;
        var ignoreExceptionHandlingAttributes =
            methodInfo?.GetCustomAttributes(typeof(IgnoreExceptionHandlingAttribute), true);

        return ignoreExceptionHandlingAttributes != null && ignoreExceptionHandlingAttributes.Length != 0;
    }
}
