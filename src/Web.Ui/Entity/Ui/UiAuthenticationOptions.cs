using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;

namespace Anarchy.Toolkit.Web.Ui.Entity.Ui;

/// <summary>
/// Cookie session and OAuth login settings of a UI application.
/// </summary>
public record UiAuthenticationOptions
{
    /// <summary>
    /// The login page for unauthenticated users.
    /// </summary>
    public string LoginPath { get; init; } = "/ui/login";

    /// <summary>
    /// The endpoint the login form posts its client credentials to.
    /// </summary>
    public string LoginEndpointPath { get; init; } = "/endpoint/auth/login";

    /// <summary>
    /// The logout endpoint; it clears the auth cookie and redirects to <see cref="LoginPath"/>.
    /// </summary>
    public string LogoutPath { get; init; } = "/endpoint/auth/logout";

    /// <summary>
    /// Where users land after a successful login.
    /// </summary>
    public string HomePath { get; init; } = "/ui";

    /// <summary>
    /// OAuth scope requested when exchanging the user's client credentials. When empty, no scope is sent and the
    /// auth service grants every scope allowed for the client.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Environments a session can target. Outside local hosts the host's own environment is always used, so it must
    /// be listed here; locally the login page lets the user pick any of them.
    /// </summary>
    public IReadOnlyCollection<UiEnvironment> Environments { get; init; } = [];

    /// <summary>
    /// Timeout of the token exchange with the auth service.
    /// </summary>
    public TimeSpan AuthServiceTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to use HTTPS-only cookies.
    /// </summary>
    public bool HttpOnly { get; init; } = true;

    /// <summary>
    /// Whether the cookie is essential for basic functionality.
    /// </summary>
    public bool IsEssential { get; init; } = true;

    /// <summary>
    /// The cookie secure policy (Strict, SameAsRequest, etc.).
    /// </summary>
    public CookieSecurePolicy SecurePolicy { get; init; } = CookieSecurePolicy.SameAsRequest;

    /// <summary>
    /// Same site mode for cookies.
    /// </summary>
    public SameSiteMode SameSite { get; init; } = SameSiteMode.Lax;

    /// <summary>
    /// Enable cookie authentication.
    /// </summary>
    public bool EnableCookieAuth { get; init; } = true;
}
