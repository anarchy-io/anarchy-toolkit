using Microsoft.AspNetCore.Http;
using System.Net.Mime;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
    /// /health/services, and /health/status).
    /// </summary>
    public abstract class ApplicationHealthMiddlewareBase
    {
        /// <summary>
        /// Create a new instance of <see cref="ApplicationHealthMiddlewareBase"/>.
        /// </summary>
        public ApplicationHealthMiddlewareBase(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Handle a HTTP request.
        /// </summary>
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

        private bool IsInstanceHealthRequest(HttpContext context) => context.Request.Path == _instanceHealthPath;
        private bool IsApplicationHealthRequest(HttpContext context) => context.Request.Path == _applicationHealthPath;
        private bool IsApplicationInformationRequest(HttpContext context) => context.Request.Path == _applicationInformationPath;

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

        private static readonly PathString _instanceHealthPath = new("/health/instance");
        private static readonly PathString _applicationHealthPath = new("/health/services");
        private static readonly PathString _applicationInformationPath = new("/health/status");
        private readonly RequestDelegate _next;
    }
}
