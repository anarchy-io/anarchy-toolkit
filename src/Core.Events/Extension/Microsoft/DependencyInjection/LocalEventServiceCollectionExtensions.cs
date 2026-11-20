using Anarchy.Toolkit.Core.Events.Local;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;

/// <summary>
/// Registration helpers for the in-process local event bus.
/// </summary>
/// <example>
/// <code language="csharp">
/// services
///     .AddLocalEventBus()
///     .AddScoped&lt;ILocalEventHandler&lt;UserRegistered&gt;, SendWelcomeEmailHandler&gt;();
/// </code>
/// </example>
public static class LocalEventServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILocalEventBus"/> (implemented by <see cref="LocalEventBus"/>) as a singleton.
    /// </summary>
    /// <remarks>
    /// Handlers are not registered by this method: register each <see cref="ILocalEventHandler{TEvent}"/>
    /// separately. <see cref="LocalEventBus"/> also depends on <c>AppTelemetry</c>, which must be
    /// registered in the container.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddLocalEventBus(this IServiceCollection services)
    {
        return services
                .AddSingleton<ILocalEventBus, LocalEventBus>()
            ;
    }
}
