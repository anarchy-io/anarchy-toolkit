using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status), resolving the <see cref="ApplicationHealthApi"/> from a new scope of
/// another container per request. Lets a self-hosted health API run the scoped service health reports
/// registered in the worker's own container.
/// </summary>
public class ApplicationHealthMiddlewareWithScopedServices : ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddlewareWithScopedServices"/>.
    /// </summary>
    public ApplicationHealthMiddlewareWithScopedServices(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory) : base(next)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>
    /// Handle a HTTP request with the services of a new scope.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var healthApi = scope.ServiceProvider.GetRequiredService<ApplicationHealthApi>();

        await InvokeWithHealthApi(context, healthApi);
    }

    private readonly IServiceScopeFactory _scopeFactory;
}
