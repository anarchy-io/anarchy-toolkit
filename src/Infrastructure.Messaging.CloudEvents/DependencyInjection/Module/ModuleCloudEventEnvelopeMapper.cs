using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.DependencyInjection.Module;

/// <summary>Registers a CloudEvent envelope mapper. The application supplies the source or mapper factory.</summary>
public sealed class ModuleCloudEventEnvelopeMapper(Func<IServiceProvider, ICloudEventEnvelopeMapper> mapperFactory)
    : IDependencyInjectionModule
{
    /// <summary>Uses the default mapper with no default event source.</summary>
    public ModuleCloudEventEnvelopeMapper() : this(defaultSource: null) { }

    /// <summary>Uses the default mapper with the specified default event source.</summary>
    public ModuleCloudEventEnvelopeMapper(Uri? defaultSource) : this(_ => new CloudEventEnvelopeMapper(defaultSource)) { }

    /// <inheritdoc/>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        => services.AddCloudEventEnvelopeMapper(mapperFactory);
}
