using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring;

public class InMemorySinkTests
{
    [Fact]
    public void Emit_AddsLogEventsToMessages()
    {
        var expected = TestLogEvent.CreateMany().ToList();
        var sut = new InMemorySink();

        foreach (var logEvent in expected)
            sut.Emit(logEvent);

        sut.Messages.Should().BeEquivalentTo(expected, opt => opt.WithStrictOrdering());
    }

    [Fact]
    public void Emit_WhenRunningInParallel_AddsAllLogEventsToMessages()
    {
        const int expectedCount = 500;
        var expected = TestLogEvent.CreateMany(expectedCount).ToList();
        var sut = new InMemorySink();

        Parallel.ForEach(expected, logEvent => sut.Emit(logEvent));

        sut.Messages.Should().HaveCount(expectedCount)
           .And.BeEquivalentTo(expected, opt => opt.WithoutStrictOrdering());
    }

    [Fact]
    public void Last_GivenNoMessages_ReturnsNull()
    {
        var sut = new InMemorySink();

        sut.Last.Should().BeNull();
    }

    [Fact]
    public void Last_ReturnsLastLoggedMessage()
    {
        var logEvents = TestLogEvent.CreateMany().ToList();
        var sut = new InMemorySink();

        foreach (var logEvent in logEvents)
            sut.Emit(logEvent);

        sut.Last.Should().BeSameAs(logEvents.Last());
    }

    [Fact]
    public void DeferredRenderingProperty_ShouldBeMaterialized_WhenLogEventIsEmitted()
    {
        var propertyValue = TestData.AnyString();
        var deferredProperty = new DeferredRenderingProperty(() => propertyValue);
        var logEventProperty = new LogEventProperty("DeferredProperty", new ScalarValue(deferredProperty));
        var logEvent = new TestLogEvent(properties: [logEventProperty]);
        var sut = new InMemorySink();
        sut.Emit(logEvent);

        using var _ = new AssertionScope();
        deferredProperty.IsMaterialized.Should().BeTrue();
        sut.Last!.Properties.Should().ContainKey("DeferredProperty")
            .WhoseValue.Should().NotBeNull()
            .And.BeOfType<ScalarValue>()
            .Which.Value!.ToString().Should().Be(propertyValue);
    }
}

internal class DeferredRenderingProperty
{
    private readonly Func<string> _materialize;
    private string? _value;
    internal bool IsMaterialized;

    public DeferredRenderingProperty(Func<string> materialize)
    {
        _materialize = materialize ?? throw new ArgumentNullException(nameof(materialize));
    }

    public override string ToString()
    {
        if (!IsMaterialized)
        {
            _value = _materialize();
            IsMaterialized = true;
        }

        return _value!;
    }
}
