using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Tests.Unit.Configuration.Infrastructure.Entity.Configuration;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.DependencyInjection.Module.Configuration.Entity;

public class ModuleAppSettingsBaseTest : TestCaseBase<AppSettings>
{
    /// <summary>Every <see cref="IAppSettings"/> capability interface implemented by the test settings.</summary>
    public static TheoryData<Type> AppSettingsInterfaces() =>
        new(typeof(AppSettings)
            .GetInterfaces()
            .Where(type => typeof(IAppSettings).IsAssignableFrom(type)));

    private static ServiceProvider BuildServiceProvider()
    {
        var assembly = typeof(AppSettings).Assembly;
        var configuration = new ConfigurationBuilder()
            .AddEmbeddedJson(
                assembly,
                $"{assembly.GetName().Name}.appsettings.json",
                $"{assembly.GetName().Name}.appsettings.Development.json")
            .Build();

        return new ModuleAppSettingsTest()
            .RegisterServices(new ServiceCollection(), configuration, TestEnvironment())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Theory]
    [MemberData(nameof(AppSettingsInterfaces))]
    public void RegisterServices_ResolvesEveryAppSettingsInterface_AsTheBoundSettingsInstance(Type appSettingsInterface)
    {
        using var provider = BuildServiceProvider();

        var settings = provider.GetRequiredService<AppSettings>();

        provider.GetService(appSettingsInterface).Should().BeSameAs(settings);
    }

    [Fact]
    public void RegisterServices_BindsSettingsFromConfiguration()
    {
        using var provider = BuildServiceProvider();

        var settings = provider.GetRequiredService<AppSettings>();

        settings.Should().BeEquivalentTo(TestSettings());
        settings.Aws.EndpointSqs.Should().Be("http://localhost:4566");
    }
}
