using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Runtime.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Entity.Api.Input;
using Anarchy.Toolkit.Runtime.Web.Ui.Entity.Ui;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Anarchy.Toolkit.Runtime.Web.Ui.Service.Host.Ui.Session;

/// <summary>
/// Handles the login and logout HTTP endpoints: exchanges the posted client credentials for an access token and
/// stores it, with the selected environment, in the auth cookie.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddToolkitUiAuthentication</c> and invoked by the endpoints mapped with
/// <c>MapToolkitUiSessionEndpoints</c>.
/// </remarks>
/// <param name="authHandler">Exchanges the credentials for an access token.</param>
/// <param name="environmentCatalog">Resolves the target environment.</param>
/// <param name="hostEnvironment">The host environment.</param>
/// <param name="options">The UI authentication options (paths, scope).</param>
public class UiSessionHandler(
    UiAuthHandler authHandler,
    IUiEnvironmentCatalog environmentCatalog,
    IHostEnvironment hostEnvironment,
    UiAuthenticationOptions options)
{
    /// <summary>
    /// Validates the request, exchanges the credentials for a token against the target environment's auth service
    /// and signs the user in with a non-persistent cookie expiring with the token.
    /// </summary>
    /// <remarks>
    /// Only local hosts honour <see cref="UiLoginRequest.TargetEnvironment"/>; deployed hosts always use their own
    /// environment. On missing credentials or a failed exchange the user is redirected to the login page with a
    /// <see cref="UiLoginErrors"/> code.
    /// </remarks>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="request">The posted login request.</param>
    /// <returns>A redirect to <see cref="UiAuthenticationOptions.HomePath"/> on success, or to the login page on failure.</returns>
    /// <exception cref="KeyNotFoundException">The target environment is not configured.</exception>
    public async Task<IResult> HandleLoginAsync(
        HttpContext context,
        UiLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return RedirectToLogin(context, UiLoginErrors.InvalidCredentials);
        }

        // Only local hosts may target another environment; deployed hosts always use their own.
        var selectedEnvironment = hostEnvironment.IsLocal()
            ? request.TargetEnvironment
            : hostEnvironment.EnvironmentName;

        var environment = environmentCatalog.GetByName(selectedEnvironment);

        var token = await authHandler.ExchangeCredentialsAsync(
            environment.AuthServiceBaseUrl,
            options.Scope,
            request.ClientId,
            request.ClientSecret,
            context.RequestAborted);
        if (token is null)
        {
            return RedirectToLogin(context, UiLoginErrors.AuthFailed);
        }

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, request.ClientId),
            new Claim(ClaimTypes.Name, request.ClientId),
            new Claim(UiSessionContext.AccessTokenClaimType, token.AccessToken),
            new Claim(UiSessionContext.TargetEnvironmentClaimType, environment.Name)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn)
            });

        return Results.Redirect(context.Request.PathBase + options.HomePath);
    }

    /// <summary>Signs the user out (clears the auth cookie) and redirects to the login page.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A redirect to <see cref="UiAuthenticationOptions.LoginPath"/>.</returns>
    public async Task<IResult> HandleLogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect(context.Request.PathBase + options.LoginPath);
    }

    private IResult RedirectToLogin(HttpContext context, string error) =>
        Results.Redirect($"{context.Request.PathBase}{options.LoginPath}?{UiLoginErrors.QueryParameter}={error}");
}
