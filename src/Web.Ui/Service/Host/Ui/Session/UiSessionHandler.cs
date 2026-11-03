using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Api.Input;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;

/// <summary>
/// Handles the login and logout HTTP endpoints: exchanges the posted client credentials for an access token and
/// stores it, with the selected environment, in the auth cookie.
/// </summary>
public class UiSessionHandler(
    UiAuthHandler authHandler,
    IUiEnvironmentCatalog environmentCatalog,
    IHostEnvironment hostEnvironment,
    UiAuthenticationOptions options)
{
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

    public async Task<IResult> HandleLogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect(context.Request.PathBase + options.LoginPath);
    }

    private IResult RedirectToLogin(HttpContext context, string error) =>
        Results.Redirect($"{context.Request.PathBase}{options.LoginPath}?{UiLoginErrors.QueryParameter}={error}");
}
