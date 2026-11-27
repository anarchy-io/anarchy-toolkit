using Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module;
using Anarchy.Toolkit.Bootstrap.Aws.Service.AppSecret;
using Anarchy.Toolkit.Bootstrap.Core.Service;
using Anarchy.Toolkit.Bootstrap.Service.AppSecret;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Bootstrap.Aws.DependencyInjection.Module;

public class ModuleAwsSecretManagementTest
{
    [Theory]
    [InlineData("Development", typeof(InMemoryAppSecretService))]
    [InlineData("Production", typeof(KmsAppSecretService))]
    public void RegisterServices_UsesKmsOnlyRemotely(string environmentName, Type expected)
    {
        var services = new ServiceCollection();

        new ModuleAwsSecretManagement().RegisterServices(services, new ConfigurationBuilder().Build(),
            Mock.Of<IHostEnvironment>(e => e.EnvironmentName == environmentName));

        services.Where(d => d.ServiceType == typeof(ISecretManagementService))
            .Should().ContainSingle().Which.ImplementationType.Should().Be(expected);
    }
}
