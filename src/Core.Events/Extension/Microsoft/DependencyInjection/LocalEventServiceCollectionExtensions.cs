using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;

/// <summary>
/// Registration helpers for the in-process local event bus.
/// </summary>
public static class LocalEventServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILocalEventBus"/> (implemented by <see cref="LocalEventBus"/>) as a singleton.
    /// </summary>
    public static IServiceCollection AddLocalEventBus(this IServiceCollection services)
    {
        return services
                .AddSingleton<ILocalEventBus, LocalEventBus>()
            ;
    }
}
