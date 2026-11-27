using Anarchy.Toolkit.Bootstrap.Core.Service;
using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Entity;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Bootstrap.DependencyInjection.Module.Configuration;

public class ModuleLibConfigurationTest
{
    private sealed class AppSettings : IAppSettings { }

    private sealed class AppSettingsModule : ModuleAppSettingsBase<AppSettings> { }

    [Fact]
    public void RegisterServices_ComposesTelemetryCorrelationAndApplicationContext()
    {
        var services = new ServiceCollection();
        services.UseModule(new ModuleLibConfiguration<AppSettingsModule, AppSettings>(),
            new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<AppTelemetry>().Should().BeOfType<AppTelemetry>();
        provider.GetRequiredService<IApplicationContext>().Should().NotBeNull();
        provider.GetRequiredService<CorrelationIdOptions>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void RegisterServices_RegistersTheInMemorySecretServiceOnlyLocally(string environmentName, bool registered)
    {
        var services = new ServiceCollection();
        services.UseModule(new ModuleLibConfiguration<AppSettingsModule, AppSettings>(),
            new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>(e => e.EnvironmentName == environmentName));

        services.Any(d => d.ServiceType == typeof(ISecretManagementService)).Should().Be(registered);
    }

    [Fact]
    public void WithSecretManagement_RegistersTheGivenModuleBeforeTheLocalFallback()
    {
        var secretManagement = new Mock<IDependencyInjectionModule>();
        secretManagement
            .Setup(m => m.RegisterServices(It.IsAny<IServiceCollection>(), It.IsAny<IConfiguration>(), It.IsAny<IHostEnvironment>()))
            .Returns((IServiceCollection services, IConfiguration _, IHostEnvironment _) =>
                services.AddSingleton(Mock.Of<ISecretManagementService>()));
        var collection = new ServiceCollection();

        collection.UseModule(
            new ModuleLibConfiguration<AppSettingsModule, AppSettings>().WithSecretManagement(secretManagement.Object),
            new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>(e => e.EnvironmentName == "Development"));

        collection.Should().ContainSingle(d => d.ServiceType == typeof(ISecretManagementService))
            .Which.ImplementationInstance.Should().NotBeNull();
    }
}
