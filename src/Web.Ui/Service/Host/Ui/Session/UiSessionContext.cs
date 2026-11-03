using Anarchy.Toolkit.Web.Ui.Core.Service.Ui.Session;
using Microsoft.AspNetCore.Components.Authorization;

namespace Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;

/// <summary>
/// Reads the UI session from the claims of the auth cookie issued by <see cref="UiSessionHandler"/>.
/// </summary>
public sealed class UiSessionContext(
    AuthenticationStateProvider authenticationStateProvider,
    IHostEnvironment hostEnvironment) : IUiSessionContext
{
    public const string AccessTokenClaimType = "access_token";
    public const string TargetEnvironmentClaimType = "ui_target_environment";

    public async Task<bool> IsAuthenticatedAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated == true;

    public async Task<string?> GetAccessTokenAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User.FindFirst(AccessTokenClaimType)?.Value;

    public async Task<string?> GetClientNameAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name;

    public async Task<string> GetTargetEnvironmentAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(TargetEnvironmentClaimType)?.Value
               ?? hostEnvironment.EnvironmentName;
    }
}
