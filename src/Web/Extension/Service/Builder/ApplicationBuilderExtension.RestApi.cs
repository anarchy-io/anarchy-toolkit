using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Service.Host.Middleware;

namespace Anarchy.Toolkit.Web.Extension.Service.Builder;

public static partial class ApplicationBuilderExtension
{
    extension(IApplicationBuilder app)
    {
        private IApplicationBuilder UseToolkitSwagger(string apiName,
                                                      IHostEnvironment environment,
                                                      string titlePrefix)
        {
            var baseUri = environment.IsLocal() ? string.Empty : "/" + environment.EnvironmentName;

            return app
                .UseSwagger()
                .UseSwaggerUI(c => {
                    c.SwaggerEndpoint("spec/swagger.json", "Default");
                    c.DocumentTitle = $"{titlePrefix} {apiName}";
                    c.RoutePrefix = "swagger";
                    c.HeadContent = $"<link rel='icon' type='image/png' href='{baseUri}/favicon.png'>";
                });
        }

        private IApplicationBuilder UseToolkitMiddlewaresRest(IHostEnvironment environment)
        {
            return app
                    .UseToolkitCommonMiddlewares(environment)
                    .UseMiddleware<LoggingMiddleware>()
                    .UseMiddleware<ExceptionMiddleware>() // Exception Middleware -> must be between UseRouting and UseEndpoints
                ;
        }

        private IApplicationBuilder UseToolkitEndpointsRest()
        {
            return app.UseEndpoints(routeBuilder => {
                routeBuilder.MapGet("/", context => {
                    context.Response.Redirect("swagger");
                    return Task.CompletedTask;
                });

                routeBuilder.MapControllers();
            });
        }

        /// <summary>
        /// Configures the toolkit REST API pipeline.
        /// Includes static files, Swagger, middleware, and endpoints.
        /// </summary>
        /// <param name="environment">The hosting environment.</param>
        /// <param name="apiName">The API name.</param>
        /// <param name="titlePrefix">Bracketed prefix for the Swagger title (e.g. "[API]").</param>
        public IApplicationBuilder UseToolkitHostRestApi(IHostEnvironment environment,
                                                         string apiName,
                                                         string titlePrefix = "[API]")
        {
            return app
                    .UseToolkitStaticFiles()
                    .UseToolkitSwagger(apiName, environment, titlePrefix)
                    .UseToolkitMiddlewaresRest(environment)
                    .UseToolkitEndpointsRest()
                ;
        }
    }
}
