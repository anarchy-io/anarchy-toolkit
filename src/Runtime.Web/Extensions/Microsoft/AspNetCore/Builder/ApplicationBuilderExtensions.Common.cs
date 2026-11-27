using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extensions.Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Builder;

public static partial class ApplicationBuilderExtensions
{
    private const string SharedWebStaticAssetsRoot = "Anarchy.Toolkit.Runtime.Web.wwwroot";

    extension(IApplicationBuilder app)
    {
        private IApplicationBuilder UseToolkitLocalDevExceptionPage(IHostEnvironment environment)
        {
            return environment.IsLocal() ? app.UseDeveloperExceptionPage() : app;
        }

        /// <summary>
        /// Serves the shared static assets embedded in this package (favicons and logos).
        /// </summary>
        /// <remarks>
        /// Files are read from the <c>Anarchy.Toolkit.Runtime.Web.wwwroot</c> embedded resources of this assembly through an
        /// <c>EmbeddedFileProvider</c>.
        /// </remarks>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitStaticFiles()
        {
            return app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new EmbeddedFileProvider(
                    typeof(ApplicationBuilderExtensions).Assembly,
                    SharedWebStaticAssetsRoot
                )
            });
        }

        /// <summary>
        /// Registers the middlewares shared by every toolkit host (correlation id, HTTPS redirection,
        /// compression, routing, authentication and authorization). Endpoints are not mapped.
        /// </summary>
        /// <remarks>
        /// The AWS X-Ray trace id middleware is not part of it: the Lambda entry point of
        /// <c>Anarchy.Toolkit.Runtime.Web.Aws</c> adds it, and other hosts opt in with its <c>UseAmazonTraceId()</c>. In the local environment
        /// the developer exception page is enabled. Authentication runs before authorization so
        /// <see cref="HttpContext.User"/> is populated. Response compression requires the providers registered by
        /// <see cref="Extensions.DependencyInjection.ServiceCollectionExtensions.AddCompressionProviders"/>.
        /// </remarks>
        /// <param name="environment">The hosting environment.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitCommonMiddlewares(IHostEnvironment environment)
        {
            return HttpsPolicyBuilderExtensions.UseHttpsRedirection(app.UseCorrelationId())
                .UseResponseCompression()
                .UseToolkitLocalDevExceptionPage(environment)
                .UseRouting()
                .UseAuthentication() // Must run before UseAuthorization to populate HttpContext.User from auth cookies
                .UseAuthorization()
                ;
        }
    }
}
