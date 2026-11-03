using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Anarchy.Toolkit.Web.Service.Host.Authentication;

public static class JwtBearerEventsBuilder
{
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
