using Anarchy.Toolkit.Core.Observability.Service.Observability;
using AwesomeAssertions;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.Core.Service.Observability;

public class ObserverTests
{
    [Fact]
    public void CreateWithNullLoggerShouldThrow()
    {
        Action create = () => new Observer(
                            logger: null!,
                            measurer: _measurer
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateWithNullMeasurerShouldThrow()
    {
        Action create = () => new Observer(
                            logger: _logger,
                            measurer: null!
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateAssignsProperties()
    {
        var sut = new Observer(
            logger: _logger,
            measurer: _measurer
        );

        sut.Logger.Should().BeSameAs(_logger);
        sut.MeasurerBase.Should().BeSameAs(_measurer);
        sut.LogContext.Should().NotBeNull();
    }

    [Fact]
    public void LogContextPushProperty_ReturnsIDisposable()
    {
        var sut = new Observer(
            logger: _logger,
            measurer: _measurer
        );

        using var disposable =
            sut.LogContext.PushProperty(new LogContextProperty("property name", "property value"));

        disposable.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_DisposesLoggerSinks()
    {
        var sink = new DisposableSink();
        var sut = new Observer(
            logger: new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger(),
            measurer: _measurer
        );

        sut.Dispose();

        sink.IsDisposed.Should().BeTrue();
    }

    private sealed class DisposableSink : Serilog.Core.ILogEventSink, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Emit(Serilog.Events.LogEvent logEvent)
        {
        }

        public void Dispose() => IsDisposed = true;
    }

    private readonly ILogger _logger = new LoggerConfiguration().CreateLogger();
    private readonly MeasurerBase _measurer = new DummyMeasurer();

    private class DummyMeasurer : MeasurerBase
    {
        public override void IncrementCounter(string name, params string[] tags)
        {
        }

        public override void IncrementCounter(string name, int amount, params string[] tags)
        {
        }

        public override void Gauge(string name, double value, params string[] tags)
        {
        }

        public override void Histogram(string name, double value, params string[] tags)
        {
        }

        public override void Timer(string name, TimeSpan elapsedTime, params string[] tags)
        {
        }

        public override void Event(string title, string text, params string[] tags)
        {
        }
    }
}
