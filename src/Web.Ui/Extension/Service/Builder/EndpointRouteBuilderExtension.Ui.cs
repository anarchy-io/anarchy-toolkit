using Anarchy.Toolkit.Web.Ui.Entity.Api.Input;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;

public static class EndpointRouteBuilderExtension
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the login (form POST, antiforgery-protected) and logout endpoints at the paths configured in
        /// <see cref="UiAuthenticationOptions"/>.
        /// </summary>
        public IEndpointRouteBuilder MapToolkitUiSessionEndpoints()
        {
            var options = endpoints.ServiceProvider.GetRequiredService<UiAuthenticationOptions>();

            endpoints
                .MapPost(options.LoginEndpointPath,
                    (HttpContext context,
                     UiSessionHandler sessionHandler,
                     [FromForm(Name = "clientId")] string? clientId,
                     [FromForm(Name = "clientSecret")] string? clientSecret,
                     [FromForm(Name = "targetEnvironment")] string? targetEnvironment) =>
                        sessionHandler.HandleLoginAsync(
                            context,
                            new UiLoginRequest(clientId ?? string.Empty, clientSecret ?? string.Empty, targetEnvironment)))
                .AllowAnonymous();

            endpoints
                .MapGet(options.LogoutPath,
                    (HttpContext context, UiSessionHandler sessionHandler) => sessionHandler.HandleLogoutAsync(context))
                .AllowAnonymous();

            return endpoints;
        }
    }
}
