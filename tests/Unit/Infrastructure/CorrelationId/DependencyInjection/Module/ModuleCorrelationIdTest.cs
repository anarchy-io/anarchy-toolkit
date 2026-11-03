using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.CorrelationId.DependencyInjection.Module;

public class ModuleCorrelationIdTest
{
    private static CorrelationIdOptions Resolve(ModuleCorrelationId module, IConfiguration configuration)
    {
        using var provider = new ServiceCollection()
            .UseModule(module, configuration, Mock.Of<IHostEnvironment>())
            .BuildServiceProvider();

        return provider.GetRequiredService<CorrelationIdOptions>();
    }

    [Fact]
    public void RegisterServices_WithoutFactory_RegistersDefaultOptions()
    {
        var options = Resolve(new ModuleCorrelationId(), new ConfigurationBuilder().Build());

        options.Should().BeEquivalentTo(new CorrelationIdOptions());
    }

    [Fact]
    public void RegisterServices_WithFactory_BindsOptionsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["CorrelationId:CorrelationIdHeaderName"] = "X-Correlation-Id",
                ["CorrelationId:Behavior"] = nameof(CorrelationIdBehavior.ThrowIfMissing),
            })
            .Build();

        var options = Resolve(
            new ModuleCorrelationId(config => config.GetSection("CorrelationId").Get<CorrelationIdOptions>()!),
            configuration);

        options.CorrelationIdHeaderName.Should().Be("X-Correlation-Id");
        options.Behavior.Should().Be(CorrelationIdBehavior.ThrowIfMissing);
    }
}
