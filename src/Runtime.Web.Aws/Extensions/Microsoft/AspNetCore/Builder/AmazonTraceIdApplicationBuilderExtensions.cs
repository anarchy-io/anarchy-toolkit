using Anarchy.Toolkit.Bootstrap.Aws.Telemetry;
using Anarchy.Toolkit.Core.Telemetry;

namespace Anarchy.Toolkit.Runtime.Web.Aws.Extensions.Microsoft.AspNetCore.Builder;

/// <summary>
/// AWS extension members for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class AmazonTraceIdApplicationBuilderExtensions
{
    /// <summary>The header carrying the AWS X-Ray trace id.</summary>
    public const string TraceIdHeader = "X-Amzn-Trace-Id";

    /// <param name="app">The application builder.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds the AWS X-Ray trace id middleware.
        /// </summary>
        /// <remarks>
        /// Reads the <c>X-Amzn-Trace-Id</c> request header into <see cref="AmazonRequestContext"/> and echoes the trace
        /// id back on the response when the response has not started yet; while the request runs, the trace id is also
        /// pushed onto the log context (<c>X-Amzn-Trace-Id</c>), for every logging provider.
        /// <see cref="AwsWebAppEntryPointBase{T}"/> adds it in front of the pipeline; other hosts (for example a container
        /// behind an AWS load balancer) call it before <c>UseToolkitCommonMiddlewares</c>.
        /// </remarks>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseAmazonTraceId()
        {
            return app.Use(async (context, next) => {
                context.Request.Headers.TryGetValue(TraceIdHeader, out var values);
                IDisposable? traceScope = null;
                if (values.Count != 0)
                {
                    AmazonRequestContext.SetValues(values[0]);
                    traceScope = context.RequestServices.GetService<AppTelemetry>()?.LogContext
                        .PushProperty(TraceIdHeader, values[0]);
                }

                using (traceScope)
                    await next.Invoke();

                if (!context.Response.HasStarted)
                {
                    context.Response.Headers.Append(TraceIdHeader, AmazonRequestContext.AmazonTraceId);
                }
            });
        }
    }
}
