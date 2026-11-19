using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Web.Service.Host.Authentication;

/// <summary>
/// Builds the <see cref="JwtBearerEvents"/> used by the toolkit JWT bearer authentication, logging token validation
/// successes, authentication failures and challenges through the
/// <see cref="IAppTelemetry"/> resolved from request services.
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
                    .GetRequiredService<IAppTelemetry>()
                    .Log(
                        LogLevel.Information,
                        context.CalledFrom(),
                        null,
                        "JWT authentication succeeded, client '{ClientId}' authorized",
                        context.Principal?.Identity?.Name ?? "unknown");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context => {
                context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<IAppTelemetry>()
                    .Log(
                        LogLevel.Error,
                        context.CalledFrom(),
                        context.Exception,
                        "JWT authentication failed");
                return Task.CompletedTask;
            },
            OnChallenge = context => {
                context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<IAppTelemetry>()
                    .Log(
                        LogLevel.Warning,
                        context.CalledFrom(),
                        null,
                        "JWT challenge triggered. Error: {Error}, Description: {Description}",
                        context.Error,
                        context.ErrorDescription);

                return Task.CompletedTask;
            }
        };
    }
}
