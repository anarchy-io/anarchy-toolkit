using Anarchy.Toolkit.Runtime.Web.Ui.Core.Service.Ui.Session;
using Microsoft.AspNetCore.Components.Authorization;

namespace Anarchy.Toolkit.Runtime.Web.Ui.Service.Host.Ui.Session;

/// <summary>
/// Reads the UI session from the claims of the auth cookie issued by <see cref="UiSessionHandler"/>.
/// </summary>
/// <param name="authenticationStateProvider">Provides the current user's authentication state.</param>
/// <param name="hostEnvironment">The host environment, used as fallback target environment.</param>
public sealed class UiSessionContext(
    AuthenticationStateProvider authenticationStateProvider,
    IHostEnvironment hostEnvironment) : IUiSessionContext
{
    /// <summary>Claim type storing the OAuth access token.</summary>
    public const string AccessTokenClaimType = "access_token";
    /// <summary>Claim type storing the environment selected at login.</summary>
    public const string TargetEnvironmentClaimType = "ui_target_environment";

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User.FindFirst(AccessTokenClaimType)?.Value;

    /// <inheritdoc/>
    public async Task<string?> GetClientNameAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name;

    /// <inheritdoc/>
    public async Task<string> GetTargetEnvironmentAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(TargetEnvironmentClaimType)?.Value
               ?? hostEnvironment.EnvironmentName;
    }
}
