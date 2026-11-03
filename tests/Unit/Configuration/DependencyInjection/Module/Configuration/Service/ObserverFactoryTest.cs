using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;
using Anarchy.Toolkit.Configuration.Host;
using Anarchy.Toolkit.Configuration.Service.Observability;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.DependencyInjection.Module.Configuration.Service;

public class ObserverFactoryTest
{
    private static IHostEnvironment Environment(string name) =>
        Mock.Of<IHostEnvironment>(e => e.EnvironmentName == name && e.ApplicationName == "Orders.Api");

    private static ObserverContext Context(string environmentName, AppRunner.Types? runner = null) =>
        new(new ServiceCollection().BuildServiceProvider(),
            new ConfigurationBuilder().Build(),
            Environment(environmentName),
            "Orders.Api",
            runner);

    [Fact]
    public void Create_Locally_UsesTheConsoleMeasurer()
    {
        using var observer = new ObserverFactory().Create(Context(Environments.Development));

        observer.MeasurerBase.Should().BeOfType<ConsoleMeasurer>();
    }

    [Theory]
    [InlineData(AppRunner.Types.Lambda)]
    [InlineData(AppRunner.Types.Ec2Worker)]
    public void Create_WhenHosted_WritesMetricsAsLogEvents(AppRunner.Types runner)
    {
        using var observer = new ObserverFactory().Create(Context("Testing", runner));

        observer.MeasurerBase.Should().BeOfType<LoggerMeasurer>();
    }

    [Fact]
    public void Create_WhenHosted_UsesTheOverriddenSteps()
    {
        var factory = new SinkObserverFactory();

        using var observer = factory.Create(Context("Production", AppRunner.Types.Lambda));
        observer.Logger.Information("hello");

        observer.MeasurerBase.Should().BeOfType<DummyMeasurer>();
        factory.Sink.Should().HaveMessage("hello")
            .Which.Should().HaveStringProperty("organisation").Which.Should().Be("acme");
        factory.Sink.Should().HaveMessage("hello")
            .Which.Should().HaveStringProperty("app_name").Which.Should().Be("Orders.Api");
    }

    [Fact]
    public void ModuleObservability_RegistersTheObserverFromTheGivenFactoryAndCorrelationOptions()
    {
        var factory = new SinkObserverFactory();
        var configuration = new ConfigurationBuilder().Build();

        using var provider = new ServiceCollection()
            .UseModule(new ModuleObservability(factory, _ => new CorrelationIdOptions { CorrelationIdHeaderName = "X-Acme" }),
                configuration,
                Environment("Production"))
            .BuildServiceProvider();

        provider.GetRequiredService<Observer>().MeasurerBase.Should().BeOfType<DummyMeasurer>();
        provider.GetRequiredService<CorrelationIdOptions>().CorrelationIdHeaderName.Should().Be("X-Acme");
    }

    private sealed class SinkObserverFactory : ObserverFactory
    {
        public InMemorySink Sink { get; } = new();

        protected override LoggerConfiguration Enrich(LoggerConfiguration configuration, ObserverContext context) =>
            base.Enrich(configuration, context).Enrich.WithProperty("organisation", "acme");

        protected override LoggerConfiguration WriteTo(LoggerConfiguration configuration, ObserverContext context) =>
            configuration.WriteTo.Sink(Sink);

        protected override MeasurerBase CreateMeasurer(ObserverContext context, ILogger logger) => new DummyMeasurer();
    }

    private sealed class DummyMeasurer : MeasurerBase
    {
        public override void IncrementCounter(string name, params string[] tags) { }
        public override void IncrementCounter(string name, int amount, params string[] tags) { }
        public override void Gauge(string name, double value, params string[] tags) { }
        public override void Histogram(string name, double value, params string[] tags) { }
        public override void Timer(string name, TimeSpan elapsedTime, params string[] tags) { }
        public override void Event(string title, string text, params string[] tags) { }
    }
}
