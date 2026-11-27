using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Extensions.Microsoft.Extensions.DependencyInjection;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.DependencyInjection.Module;

/// <summary>Registers Kafka using producer configuration supplied by the composition root.</summary>
public sealed class ModuleKafkaMessageBroker(Func<IConfiguration, ProducerConfig> options) : IDependencyInjectionModule
{
    /// <inheritdoc/>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services.AddKafkaMessageBroker(options(configuration));
}
