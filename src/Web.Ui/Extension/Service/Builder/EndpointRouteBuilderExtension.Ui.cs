using Anarchy.Toolkit.Web.Ui.Entity.Api.Input;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;
using Microsoft.AspNetCore.Mvc;

namespace Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;

/// <summary>
/// <see cref="IEndpointRouteBuilder"/> extensions mapping the toolkit UI HTTP endpoints.
/// </summary>
public static class EndpointRouteBuilderExtension
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the toolkit UI endpoints: the session login/logout endpoints when the cookie session is registered
        /// with cookie authentication enabled; nothing otherwise.
        /// </summary>
        /// <returns>The endpoint route builder, for chaining.</returns>
        public IEndpointRouteBuilder MapToolkitUi()
        {
            if (endpoints.ServiceProvider.GetService<UiAuthenticationOptions>() is { EnableCookieAuth: true })
            {
                endpoints.MapToolkitUiSessionEndpoints();
            }

            return endpoints;
        }

        /// <summary>
        /// Maps the login (form POST, antiforgery-protected) and logout endpoints at the paths configured in
        /// <see cref="UiAuthenticationOptions"/>.
        /// </summary>
        /// <remarks>
        /// The login endpoint reads the <c>clientId</c>, <c>clientSecret</c> and <c>targetEnvironment</c> form fields
        /// and delegates to <see cref="UiSessionHandler.HandleLoginAsync"/>; the logout endpoint (GET) delegates to
        /// <see cref="UiSessionHandler.HandleLogoutAsync"/>. Both allow anonymous access. Requires
        /// <c>AddToolkitUiAuthentication</c> with cookie authentication enabled; <c>UiWebAppStartupBase</c> calls it
        /// automatically in that case.
        /// </remarks>
        /// <returns>The endpoint route builder, for chaining.</returns>
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
