using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Observability;
using AwesomeAssertions;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.CorrelationId.Service.Observability;

public class CorrelationIdEnricherTests
{
    private const string PropertyNameCorrelationId = "CorrelationId";
    private const string PropertyNameAttemptId = "AttemptId";

    private readonly TestLogEvent _logEvent = new();
    // CorrelationIdEnricher is internal and the toolkit does not expose internals to this test assembly,
    // so it is created via reflection (the legacy test project used InternalsVisibleTo).
    private readonly ILogEventEnricher _sut = CreateEnricher();
    private readonly Mock<ILogEventPropertyFactory> _propertyFactoryMock = new(MockBehavior.Strict);

    [Fact]
    public void Enrich_SetsCorrelationId()
    {
        var expected = Guid.NewGuid().ToString("N");
        GivenFactoryReturns(correlationId: expected);

        _sut.Enrich(_logEvent, _propertyFactoryMock.Object);

        _logEvent.Properties.Should().ContainKey(PropertyNameCorrelationId)
                 .WhoseValue.Should().BeEquivalentTo(new ScalarValue(expected));
    }

    [Fact]
    public void Enrich_SetsAttemptId()
    {
        var expected = Guid.NewGuid().ToString("N");
        GivenFactoryReturns(attemptId: expected);

        _sut.Enrich(_logEvent, _propertyFactoryMock.Object);

        _logEvent.Properties.Should().ContainKey(PropertyNameAttemptId)
                 .WhoseValue.Should().BeEquivalentTo(new ScalarValue(expected));
    }

    [Fact]
    public void Enrich_GivenAlreadySet_DoesNotUpdateCorrelationId()
    {
        var oldValue = Guid.NewGuid().ToString("N");
        var newValue = Guid.NewGuid().ToString("N");
        GivenFactoryReturns(correlationId: newValue);
        _logEvent.AddPropertyIfAbsent(GivenLogEventProperty(PropertyNameCorrelationId, oldValue));

        _sut.Enrich(_logEvent, _propertyFactoryMock.Object);

        _logEvent.Properties.Should().ContainKey(PropertyNameCorrelationId)
                 .WhoseValue.Should().BeEquivalentTo(new ScalarValue(oldValue));
    }

    [Fact]
    public void Enrich_GivenAlreadySet_DoesNotUpdateAttemptId()
    {
        var oldValue = Guid.NewGuid().ToString("N");
        var newValue = Guid.NewGuid().ToString("N");
        GivenFactoryReturns(attemptId: newValue);
        _logEvent.AddPropertyIfAbsent(GivenLogEventProperty(PropertyNameAttemptId, oldValue));

        _sut.Enrich(_logEvent, _propertyFactoryMock.Object);

        _logEvent.Properties.Should().ContainKey(PropertyNameAttemptId)
                 .WhoseValue.Should().BeEquivalentTo(new ScalarValue(oldValue));
    }

    private void GivenFactoryReturns(string? correlationId = null, string? attemptId = null)
    {
        _propertyFactoryMock
           .Setup(x => x.CreateProperty(PropertyNameCorrelationId, It.IsAny<string?>(), false))
           .Returns(
                GivenLogEventProperty(
                    name: PropertyNameCorrelationId,
                    value: correlationId ?? Guid.NewGuid().ToString("N")
                )
            );
        _propertyFactoryMock
           .Setup(x => x.CreateProperty(PropertyNameAttemptId, It.IsAny<string?>(), false))
           .Returns(
                GivenLogEventProperty(
                    name: PropertyNameAttemptId,
                    value: attemptId ?? Guid.NewGuid().ToString("N")
                )
            );
    }

    private static LogEventProperty GivenLogEventProperty(string name, string? value) =>
        new(
            name,
            new ScalarValue(value)
        );

    private static ILogEventEnricher CreateEnricher()
    {
        var enricherType = typeof(EnvironmentLoggerConfigurationExtensions).Assembly
           .GetType("Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Observability.CorrelationIdEnricher", throwOnError: true)!;

        return (ILogEventEnricher)Activator.CreateInstance(enricherType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
            binder: null, args: [PropertyNameCorrelationId, PropertyNameAttemptId], culture: null)!;
    }

    private class TestLogEvent : LogEvent
    {
        public TestLogEvent() : base(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            new Exception(),
            new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()),
            Enumerable.Empty<LogEventProperty>()
        )
        {
        }
    }
}
