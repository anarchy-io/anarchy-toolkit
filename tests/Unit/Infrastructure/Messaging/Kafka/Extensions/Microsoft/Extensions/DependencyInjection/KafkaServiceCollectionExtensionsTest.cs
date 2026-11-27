using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Service.MessageBroker;
using AwesomeAssertions;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Kafka.Extensions.Microsoft.Extensions.DependencyInjection;

public class KafkaServiceCollectionExtensionsTest
{
    [Fact]
    public void AddKafkaMessageBroker_RegistersSingletonsAndContainerDisposesProducer()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(AppTelemetry.Null);
        services.AddKafkaMessageBroker(new ProducerConfig { BootstrapServers = "localhost:9092" });
        var producerRegistration = services.Single(d => d.ServiceType == typeof(IProducer<string, string>));
        producerRegistration.Lifetime.Should().Be(ServiceLifetime.Singleton);
        producerRegistration.ImplementationFactory.Should().NotBeNull();

        // Replace the producer factory to exercise container ownership without starting a Kafka client.
        var producer = new Mock<IProducer<string, string>>();
        services.Remove(producerRegistration);
        services.AddSingleton<IProducer<string, string>>(_ => producer.Object);
        using (var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }))
        {
            var publisher = provider.GetRequiredService<KafkaMessageBrokerPublisher>();
            provider.GetRequiredKeyedService<IMessageBrokerPublisher>("kafka").Should().BeSameAs(publisher);
            provider.GetRequiredKeyedService<IMessageBrokerPublisher>("kafka").Should().BeSameAs(publisher);
            provider.GetRequiredService<IProducer<string, string>>().Should().BeSameAs(producer.Object);
        }

        producer.Verify(p => p.Dispose(), Times.Once);
    }

    [Fact]
    public void AddKafkaMessageBroker_WhenConfigIsNull_ThrowsSpecificNullArgumentException()
    {
        IServiceCollection services = new ServiceCollection();
        var act = () => services.AddKafkaMessageBroker(null!);

        var exception = act.Should().ThrowExactly<KafkaNullArgumentException>().Which;
        exception.ParamName.Should().Be("config");
        exception.Should().BeAssignableTo<ArgumentNullException>();
    }
}
