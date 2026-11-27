using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status), resolving the <see cref="ApplicationHealthApi"/> from a new scope of
/// another container per request. Lets a self-hosted health API run the scoped service health reports
/// registered in the worker's own container.
/// </summary>
/// <remarks>See <see cref="ApplicationHealthMiddlewareBase"/> for the endpoint behavior.</remarks>
public class ApplicationHealthMiddlewareWithScopedServices : ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddlewareWithScopedServices"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="scopeFactory">Creates a scope per request in the container that registers <see cref="ApplicationHealthApi"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="scopeFactory"/> is <see langword="null"/>.</exception>
    public ApplicationHealthMiddlewareWithScopedServices(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory) : base(next)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>
    /// Handle a HTTP request with the services of a new scope.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    public async Task Invoke(HttpContext context)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var healthApi = scope.ServiceProvider.GetRequiredService<ApplicationHealthApi>();

        await InvokeWithHealthApi(context, healthApi);
    }

    private readonly IServiceScopeFactory _scopeFactory;
}
