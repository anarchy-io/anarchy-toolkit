using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.DependencyInjection.Module.Configuration.Service;

public class ModuleTelemetryTest
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder().Build();
    private static readonly IHostEnvironment Environment = Mock.Of<IHostEnvironment>();

    [Fact]
    public void RegisterServices_UsesHostInfrastructureAndPreservesCustomFacade()
    {
        var existing = Mock.Of<IAppTelemetry>();
        var logging = Mock.Of<ILoggerProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(existing);
        services.AddSingleton(logging);

        services.UseModule(new ModuleTelemetry(), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Should().BeSameAs(existing);
        provider.GetServices<ILoggerProvider>().Should().Contain(logging);
    }

    [Fact]
    public void RegisterServices_CreatesDefaultFacadeWithoutAdditionalSetup()
    {
        var services = new ServiceCollection();
        services.UseModule(new ModuleTelemetry(), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Should().BeOfType<AppTelemetry>();
    }

    [Fact]
    public void LegacyModule_DoesNotRemoveExistingLoggingProviders()
    {
        var logging = Mock.Of<ILoggerProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(logging);

        services.UseModule(new ModuleObservability(), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetServices<ILoggerProvider>().Should().Contain(logging);
    }

    [Fact]
    public void ModuleTelemetry_PreservesTheConsumerSerilogSink()
    {
        var sink = new InMemorySink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.UseModule(new ModuleTelemetry(), Configuration, Environment);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IAppTelemetry>().Log(LogLevel.Information, new CallSite("Orders", "Handle"), null,
            "Processing {OrderId}", 42);

        sink.Messages.Should().ContainSingle().Which.RenderMessage().Should().Contain("Processing 42");
    }
}
