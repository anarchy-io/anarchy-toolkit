using Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;
using Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Service.Event.InMemory;

public class LocalEventBusTest
{
    [Fact]
    public async Task PublishAsync_ReportsHandlerFailureThroughTheInjectedFacade()
    {
        var telemetry = new Mock<IAppTelemetry>();
        var failure = new InvalidOperationException("failed");
        var handler = new Mock<ILocalEventHandler<ExampleEvent>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<ExampleEvent>(), It.IsAny<CancellationToken?>()))
            .Returns(Task.FromException(failure));
        var services = new ServiceCollection();
        services.AddSingleton(telemetry.Object);
        services.AddSingleton(handler.Object);
        services.AddLocalEventBus();
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ILocalEventBus>().PublishAsync(new ExampleEvent(), ct: TestContext.Current.CancellationToken);

        telemetry.Verify(t => t.Log(LogLevel.Error, It.Is<CallSite>(site => site.ClassName == nameof(LocalEventBus)),
            failure, "Failed to process in-memory event", It.IsAny<object?[]>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ReportsHandlerFailureToTheConsumerSerilogSink()
    {
        var sink = new InMemorySink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var handler = new Mock<ILocalEventHandler<ExampleEvent>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<ExampleEvent>(), It.IsAny<CancellationToken?>()))
            .Returns(Task.FromException(new InvalidOperationException("failed")));
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.AddSingleton(handler.Object);
        services.AddLocalEventBus();
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ILocalEventBus>().PublishAsync(new ExampleEvent(), ct: TestContext.Current.CancellationToken);

        sink.Messages.Should().ContainSingle().Which.RenderMessage().Should().Contain("Failed to process in-memory event");
    }

    public sealed class ExampleEvent : ILocalEvent;
}
