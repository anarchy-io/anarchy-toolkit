using Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Local;

public class LocalEventBusTest
{
    [Fact]
    public async Task PublishAsync_ReportsHandlerFailureThroughTheInjectedTelemetry()
    {
        using var telemetry = new TestTelemetry();
        var failure = new InvalidOperationException("failed");
        var handler = new Mock<ILocalEventHandler<ExampleEvent>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<ExampleEvent>(), It.IsAny<CancellationToken?>()))
            .Returns(Task.FromException(failure));
        var services = new ServiceCollection();
        services.AddSingleton(telemetry.Telemetry);
        services.AddSingleton(handler.Object);
        services.AddLocalEventBus();
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ILocalEventBus>().PublishAsync(new ExampleEvent(), ct: TestContext.Current.CancellationToken);

        telemetry.Logs.Should().ContainSingle().Which.Should().Match<RecordedLog>(log =>
            log.Level == LogLevel.Error && log.Exception == failure
            && log.Message.Contains($"[{nameof(LocalEventBus)} → ") && log.Message.Contains("Failed to process in-memory event"));
    }

    [Fact]
    public async Task PublishAsync_ReportsHandlerFailureToTheRegisteredLoggingProvider()
    {
        using var telemetry = new TestTelemetry();
        var handler = new Mock<ILocalEventHandler<ExampleEvent>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<ExampleEvent>(), It.IsAny<CancellationToken?>()))
            .Returns(Task.FromException(new InvalidOperationException("failed")));
        var services = new ServiceCollection();
        services.AddTestTelemetry(telemetry);
        services.AddSingleton(handler.Object);
        services.AddLocalEventBus();
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ILocalEventBus>().PublishAsync(new ExampleEvent(), ct: TestContext.Current.CancellationToken);

        telemetry.Logs.Should().ContainSingle().Which.Message.Should().Contain("Failed to process in-memory event");
    }

    public sealed class ExampleEvent : ILocalEvent;
}
