using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Entity.Event;

public abstract class EventEnvelopeTestBase(
    string expectedTypeAggregate,
    string expectedTypeEvent,
    Dictionary<string, Type?> expectedPayloadProperties)
    : TestCaseBase
{
    [Fact]
    public void Constructor_BuildsValidEventEnvelope()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();

        // Act
        var sut = Sut(aggregateId);

        // Assert
        using (new AssertionScope())
        {
            sut.AggregateId.Should().Be(aggregateId);
            sut.TypeAggregate.Should().Be(expectedTypeAggregate);
            sut.TypeEvent.Should().Be(expectedTypeEvent);

            if (sut.Payload == null)
            {
                return;
            }

            var payloadProperties = sut.Payload.GetType()
                .GetProperties()
                .ToDictionary(x => x.Name, x => x.GetValue(sut.Payload)?.GetType());

            payloadProperties.Should().BeEquivalentTo(expectedPayloadProperties);
        }
    }

    [Fact]
    public void DeepClone_BuildsValidEventEnvelope()
    {
        var sut = Sut(Guid.NewGuid().ToString());

        var clone = sut.DeepClone();

        clone.Should().NotBeSameAs(sut);
        clone.Should().BeEquivalentTo(sut, options => options.Excluding(x => x.Moment));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WhenEmptyAggregateId_ThrowsException(string? value)
    {
        // Act
        var act = () => Sut(value!, expectedTypeAggregate, expectedTypeEvent);

        // Assert
        act.Should().Throw<InvalidEventEnvelopeValueException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WhenEmptyTypeAggregate_ThrowsException(string? value)
    {
        // Act
        var act = () => Sut(TestHelper.RandomId("test."), value!, expectedTypeEvent);

        // Assert
        act.Should().Throw<InvalidEventEnvelopeValueException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WhenEmptyTypeEvent_ThrowsException(string? value)
    {
        // Act
        var act = () => Sut(TestHelper.RandomId("test."), expectedTypeAggregate, value!);

        // Assert
        act.Should().Throw<InvalidEventEnvelopeValueException>();
    }

    protected EventEnvelopeBase<IEventEnvelopePayload> Sut(string? aggregateId = null)
    {
        return Sut(aggregateId ?? TestHelper.RandomId("test."), expectedTypeAggregate, expectedTypeEvent);
    }

    protected abstract EventEnvelopeBase<IEventEnvelopePayload> Sut(
        string? aggregateId,
        string? typeAggregate,
        string? typeEvent);

    protected static Dictionary<string, Type> GetJsonPropertyTypes(string jsonString)
    {
        var propertyTypes = new Dictionary<string, Type>();

        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return propertyTypes;
        }

        foreach (var property in root.EnumerateObject())
        {
            var propertyName = property.Name;

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                    propertyTypes.Add(propertyName, typeof(string));
                    break;
                case JsonValueKind.Number:
                    propertyTypes.Add(propertyName, typeof(double));
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    propertyTypes.Add(propertyName, typeof(bool));
                    break;
                case JsonValueKind.Array:
                    propertyTypes.Add(propertyName, typeof(Array));
                    break;
                case JsonValueKind.Object:
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                default:
                    propertyTypes.Add(propertyName, typeof(object));
                    break;
            }
        }

        return propertyTypes;
    }
}
