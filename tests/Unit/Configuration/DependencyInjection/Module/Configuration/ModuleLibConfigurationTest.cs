using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.DependencyInjection.Module.Configuration;

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
}
