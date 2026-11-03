using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.DependencyInjection.Module;

public class ModuleSnsMessageBrokerTest
{
    [Fact]
    public void RegisterServices_ResolvesOptionsFromConfigurationAndRegistersKeyedPublisher()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Messaging:Region"] = "eu-west-1",
                ["Messaging:AccountId"] = "123456789012",
            })
            .Build();

        var services = new ServiceCollection().UseModule(
            new ModuleSnsMessageBroker(config => new AwsMessagingOptions(
                config["Messaging:Region"]!, config["Messaging:AccountId"]!)),
            configuration,
            Mock.Of<IHostEnvironment>());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<AwsMessagingOptions>()
            .Should().Be(new AwsMessagingOptions("eu-west-1", "123456789012"));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IMessageBrokerPublisher) && Equals(d.ServiceKey, "topic"));
    }
}
