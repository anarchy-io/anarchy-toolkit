using Anarchy.Toolkit.Runtime.Web.MockServer.Extensions.Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Samples.MockServer.Middlewares;

/// <summary>
/// Answers <c>401</c> to any <c>/payments</c> request without a bearer token, like the real service would, so
/// expectations only describe the authorized cases.
/// </summary>
public sealed class RequireBearerTokenMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/payments")
            && !context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.Ordinal))
        {
            context.GetMockServer().AnsweredBy = "bearer token middleware";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "missing bearer token" });
            return;
        }

        await next(context);
    }
}
