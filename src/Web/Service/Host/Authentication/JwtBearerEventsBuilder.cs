using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Anarchy.Toolkit.Web.Service.Host.Authentication;

/// <summary>
/// Builds the <see cref="JwtBearerEvents"/> used by the toolkit JWT bearer authentication, logging token validation
/// successes, authentication failures and challenges through the
/// <see cref="Anarchy.Toolkit.Core.Observability.Service.Observability.Observer"/> resolved from request services.
/// </summary>
public static class JwtBearerEventsBuilder
{
    /// <summary>Creates a new <see cref="JwtBearerEvents"/> instance with logging handlers.</summary>
    /// <returns>The configured events.</returns>
    public static JwtBearerEvents Build()
    {
        return new JwtBearerEvents {
            OnTokenValidated = context => {
                context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<Observer>()
                    .LogInformation(
                        context.CalledFrom(),
                        "JWT authentication succeeded, client '{ClientId}' authorized",
                        context.Principal?.Identity?.Name ?? "unknown");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context => {
                context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<Observer>()
                    .LogError(
                        context.Exception,
                        context.CalledFrom(),
                        "JWT authentication failed");
                return Task.CompletedTask;
            },
            OnChallenge = context => {
                context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<Observer>()
                    .LogWarning(
                        context.CalledFrom(),
                        "JWT challenge triggered. Error: {Error}, Description: {Description}",
                        context.Error,
                        context.ErrorDescription);

                return Task.CompletedTask;
            }
        };
    }
}
