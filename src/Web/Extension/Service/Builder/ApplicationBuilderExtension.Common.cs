using Anarchy.Toolkit.Configuration.Entity.Observability;
using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Service.Builder;
using Microsoft.Extensions.FileProviders;

namespace Anarchy.Toolkit.Web.Extension.Service.Builder;

public static partial class ApplicationBuilderExtension
{
    private const string SharedWebStaticAssetsRoot = "Anarchy.Toolkit.Web.wwwroot";

    extension(IApplicationBuilder app)
    {
        private IApplicationBuilder UseToolkitLocalDevExceptionPage(IHostEnvironment environment)
        {
            return environment.IsLocal() ? app.UseDeveloperExceptionPage() : app;
        }

        /// <summary>
        /// Serves the shared static assets embedded in this package (favicons, Swagger theme stylesheets, script and logos).
        /// </summary>
        /// <remarks>
        /// Files are read from the <c>Anarchy.Toolkit.Web.wwwroot</c> embedded resources of this assembly through an
        /// <c>EmbeddedFileProvider</c>.
        /// </remarks>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitStaticFiles()
        {
            return app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new EmbeddedFileProvider(
                    typeof(ApplicationBuilderExtension).Assembly,
                    SharedWebStaticAssetsRoot
                )
            });
        }

        /// <summary>
        /// Registers the middlewares shared by every toolkit host (trace id, correlation id, HTTPS redirection,
        /// compression, routing, authentication and authorization). Endpoints are not mapped.
        /// </summary>
        /// <remarks>
        /// The first middleware reads the <c>X-Amzn-Trace-Id</c> request header into <c>AmazonRequestContext</c> and
        /// echoes the trace id back on the response when the response has not started yet. In the local environment
        /// the developer exception page is enabled. Authentication runs before authorization so
        /// <see cref="HttpContext.User"/> is populated. Response compression requires the providers registered by
        /// <see cref="Anarchy.Toolkit.Web.Extension.DependencyInjection.ServiceCollectionExtension.AddCompressionProviders"/>.
        /// </remarks>
        /// <param name="environment">The hosting environment.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitCommonMiddlewares(IHostEnvironment environment)
        {
            return app
                .Use(async (context, next) => {
                    context.Request.Headers.TryGetValue("X-Amzn-Trace-Id", out var values);
                    if (values.Count != 0)
                    {
                        AmazonRequestContext.SetValues(values[0]);
                    }

                    await next.Invoke();

                    if (!context.Response.HasStarted)
                    {
                        context.Response.Headers.Append("X-Amzn-Trace-Id", AmazonRequestContext.AmazonTraceId);
                    }
                })
                .UseCorrelationId()
                .UseHttpsRedirection()
                .UseResponseCompression()
                .UseToolkitLocalDevExceptionPage(environment)
                .UseRouting()
                .UseAuthentication() // Must run before UseAuthorization to populate HttpContext.User from auth cookies
                .UseAuthorization()
                ;
        }
    }
}
