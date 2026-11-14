using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost;

internal class DefaultRequestHandler
{
    public static Task HandleRequestAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
}
