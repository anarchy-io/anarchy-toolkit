using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Extension.Microsoft.DependencyInjection;

/// <summary>Registers the default or an application-supplied CloudEvent mapper.</summary>
public static class CloudEventServiceCollectionExtensions
{
    /// <summary>Registers the default mapper as a singleton, unless an application mapper is already registered.</summary>
    public static IServiceCollection AddCloudEventEnvelopeMapper(this IServiceCollection services, Uri? defaultSource = null)
        => services.AddCloudEventEnvelopeMapper(_ => new CloudEventEnvelopeMapper(defaultSource));

    /// <summary>Registers an application-supplied singleton mapper factory if the interface is not yet registered.</summary>
    public static IServiceCollection AddCloudEventEnvelopeMapper(
        this IServiceCollection services, Func<IServiceProvider, ICloudEventEnvelopeMapper> mapperFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(mapperFactory);
        services.TryAddSingleton<ICloudEventEnvelopeMapper>(mapperFactory);
        return services;
    }
}
