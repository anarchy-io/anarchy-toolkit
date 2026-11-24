using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Service.MessageBroker;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Extension.Microsoft.DependencyInjection;

/// <summary>Registers a Kafka producer and publisher.</summary>
public static class KafkaServiceCollectionExtensions
{
    /// <summary>Registers a container-owned producer and a keyed <c>kafka</c> publisher. Register AppTelemetry separately.</summary>
    public static IServiceCollection AddKafkaMessageBroker(this IServiceCollection services, ProducerConfig config)
    {
        if (config is null) throw new KafkaNullArgumentException(nameof(config));
        services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(config).Build());
        services.AddSingleton<KafkaMessageBrokerPublisher>();
        services.AddKeyedSingleton<IMessageBrokerPublisher>("kafka", (sp, _) => sp.GetRequiredService<KafkaMessageBrokerPublisher>());
        return services;
    }
}
