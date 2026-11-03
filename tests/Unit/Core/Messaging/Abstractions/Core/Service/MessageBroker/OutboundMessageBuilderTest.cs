using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Messaging.Abstractions.Dummy;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Tests.Unit.Core.Messaging.Abstractions.Core.Service.MessageBroker;

public class OutboundMessageBuilderTest : TestCaseBase
{
    private readonly DummyMessageBrokerDestination _brokerDestination;
    private readonly DummyMessageBrokerSource _source;
    private readonly OutboundMessageBuilder _sut;

    public OutboundMessageBuilderTest()
    {
        _source = TestHelper.RandomEnum<DummyMessageBrokerSource>();
        _brokerDestination = TestHelper.RandomEnum<DummyMessageBrokerDestination>();
        _sut = OutboundMessageBuilder.Create(_brokerDestination, _source);
    }

    [Fact]
    public void Build_HappyFlow()
    {
        var message = TestHelper.RandomId("Message");
        var partitionKey = TestHelper.RandomId("Partition");
        var attributes = new Dictionary<string, string> {
            ["TypeAggregate"] = "dummy-aggregate",
            ["AggregateId"] = partitionKey,
            ["TypeEvent"] = "dummy-event"
        };

        var fixtureResult = _sut.Build(message, partitionKey, attributes);

        AssertOutboundMessage(_brokerDestination.ToDescription(), message, partitionKey, attributes, fixtureResult);
    }

    [Fact]
    public void Build_WhenNoSourceIsSet_DoesNotAddSourceAttribute()
    {
        var fixtureResult = new OutboundMessageBuilder(_brokerDestination.ToDescription())
            .Build("test-body", attributes: new Dictionary<string, string> { ["TestAttribute"] = "testing-the-tester" });

        fixtureResult.Attributes.Should().NotContainKey("Source");
        fixtureResult.Attributes.Should().Contain("TestAttribute", "testing-the-tester");
    }

    [Fact]
    public void Build_WhenNoAttributesAreProvided_ReturnsOnlySourceAttribute()
    {
        var fixtureResult = _sut.Build("test-body");

        using (new AssertionScope())
        {
            fixtureResult.PartitionKey.Should().BeNull();
            fixtureResult.Attributes.Should().HaveCount(1);
            fixtureResult.Attributes.Should().Contain("Source", _source.ToDescription());
        }
    }

    [Fact]
    public void Build_WhenAttributesContainSource_OverwritesItWithConfiguredSource()
    {
        var fixtureResult = _sut.Build(
            "test-body",
            attributes: new Dictionary<string, string> { ["Source"] = "some-other-source" });

        fixtureResult.Attributes.Should().Contain("Source", _source.ToDescription());
    }

    [Fact]
    public void Build_DoesNotMutateProvidedAttributes()
    {
        var attributes = new Dictionary<string, string> { ["TestAttribute"] = "testing-the-tester" };

        _sut.Build("test-body", attributes: attributes);

        attributes.Should().HaveCount(1).And.NotContainKey("Source");
    }

    [Fact]
    public void Build_WhenBuildingMultipleMessages_GeneratesUniqueIds()
    {
        const int count = 10;

        var results = Enumerable.Range(0, count)
            .Select(idx => _sut.Build(message: $"message-{idx}", partitionKey: $"partition-{idx}"))
            .ToArray();

        using (new AssertionScope())
        {
            results.Length.Should().Be(count);
            results.DistinctBy(m => m.Id).Count().Should().Be(count);
            results.DistinctBy(m => m.DeduplicationId).Count().Should().Be(count);
            results.Should().AllSatisfy(m => m.Id.Should().NotBe(m.DeduplicationId));
        }
    }

    [Fact]
    public void Build_WithStringDestination_OverridesConfiguredDestination()
    {
        var fixtureResult = _sut.Build("explicit-destination", "test-body", "test-id");

        AssertOutboundMessage(
            "explicit-destination",
            "test-body",
            "test-id",
            new Dictionary<string, string>(),
            fixtureResult);
    }

    [Fact]
    public void Build_WithEnumDestination_OverridesConfiguredDestinationWithItsDescription()
    {
        var destination = TestHelper.RandomEnum([_brokerDestination]);

        var fixtureResult = _sut.Build(destination, "test-body", "test-id");

        AssertOutboundMessage(
            destination.ToDescription(),
            "test-body",
            "test-id",
            new Dictionary<string, string>(),
            fixtureResult);
    }

    [Fact]
    public void Create_WithEnumDestinationAndStringSource_ResolvesDestinationDescription()
    {
        var fixtureResult = OutboundMessageBuilder.Create(_brokerDestination, "string-source").Build("test-body");

        fixtureResult.Destination.Should().Be(_brokerDestination.ToDescription());
        fixtureResult.Attributes.Should().Contain("Source", "string-source");
    }

    [Fact]
    public void Create_WithEnumDestinationAndNoSource_ResolvesDestinationDescription()
    {
        var fixtureResult = OutboundMessageBuilder.Create(_brokerDestination).Build("test-body");

        fixtureResult.Destination.Should().Be(_brokerDestination.ToDescription());
        fixtureResult.Attributes.Should().NotContainKey("Source");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Build_WhenMessageIsEmpty_ThrowsArgumentException(string message)
    {
        var act = () => _sut.Build(message);

        act.Should().Throw<ArgumentException>().WithParameterName("message");
    }

    [Fact]
    public void Build_WhenDestinationIsEmpty_ThrowsArgumentException()
    {
        var act = () => new OutboundMessageBuilder(string.Empty, "source").Build("test-body");

        act.Should().Throw<ArgumentException>().WithParameterName("destination");
    }

    private void AssertOutboundMessage(
        string expectedDestination,
        string expectedMessage,
        string? expectedPartitionKey,
        IReadOnlyDictionary<string, string> expectedAttributes,
        OutboundMessageEntity fixtureResult)
    {
        using (new AssertionScope())
        {
            fixtureResult.Id.Should().NotBeNullOrWhiteSpace();
            fixtureResult.DeduplicationId.Should().NotBeNullOrWhiteSpace();
            fixtureResult.Destination.Should().Be(expectedDestination);
            fixtureResult.PartitionKey.Should().Be(expectedPartitionKey);
            fixtureResult.Message.Should().Be(expectedMessage);
            fixtureResult.Attributes.Should().Contain("Source", _source.ToDescription());

            foreach (var (key, value) in expectedAttributes)
            {
                fixtureResult.Attributes.Should().Contain(key, value);
            }
        }
    }
}
