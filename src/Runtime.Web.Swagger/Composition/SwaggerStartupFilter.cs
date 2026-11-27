using Anarchy.Toolkit.Runtime.Web.Swagger.Extensions.Microsoft.AspNetCore.Builder;

namespace Anarchy.Toolkit.Runtime.Web.Swagger.Composition;

/// <summary>
/// Puts the toolkit Swagger UI (<c>UseToolkitSwagger</c>) in front of the application's pipeline.
/// </summary>
internal sealed class SwaggerStartupFilter(string apiName, string titlePrefix) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app => {
            app.UseToolkitSwagger(app.ApplicationServices.GetRequiredService<IWebHostEnvironment>(), apiName, titlePrefix);
            next(app);
        };
    }
}
