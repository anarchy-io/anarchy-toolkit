using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Events.Service.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.MessageBroker;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class EventEnvelopeParserTest
{
    [Fact]
    public async Task FromInboundMessage_ForEvent_WhenPayloadIsInvalid_ThrowsException()
    {
        var fixtureInboundMessage = new InboundMessageEntityFixture().Build();

        var result = async () =>
            await new EventEnvelopeParser<InboundMessageEntity, Event>().FromInboundMessage(fixtureInboundMessage);

        await result.Should().ThrowAsync<MessageBrokerParsingException>();
    }

    [Fact]
    public async Task FromInboundMessage_ForEvent_WhenPayloadIsValid_ReturnsValidEvent()
    {
        var fixtureEvent = new EventFixture().Build();
        var fixtureInboundMessage = new InboundMessageEntityFixture(fixtureEvent).Build();

        var result =
            await new EventEnvelopeParser<InboundMessageEntity, Event>().FromInboundMessage(fixtureInboundMessage);

        var payload = ((JObject)result.Payload!).ToObject<DummyEventPayload>();

        result.Should().BeEquivalentTo(fixtureEvent, options => options.Excluding(x => x.Payload));
        payload.Should().BeEquivalentTo(fixtureEvent.Payload, options =>
            options.Using<DateTimeOffset>(assertionContext =>
                    assertionContext.Subject.Should().BeCloseTo(assertionContext.Expectation, TimeSpan.FromMilliseconds(10)))
                .When(info => info.Path.EndsWith("LastModificationTime")));
    }

    [Fact]
    public async Task FromInboundMessage_WithPersistedEvent_WhenPayloadIsInvalid_ThrowsException()
    {
        var fixtureInboundMessage = new InboundMessageEntityFixture().Build();

        var result = async () =>
            await new EventEnvelopeParser<InboundMessageEntity, PersistedEvent>().FromInboundMessage(fixtureInboundMessage);

        await result.Should().ThrowAsync<MessageBrokerParsingException>();
    }

    [Fact]
    public async Task FromInboundMessage_WithPersistedEvent_WhenPayloadIsValid_ReturnsValidPersistedEvent()
    {
        var fixtureEvent = PersistedEvent.Create(
            TestHelper.RandomId<long>(),
            DummyTypeAggregate.DummyTask,
            TestHelper.RandomId("Test.AggregateId"),
            DummyTypeEvent.DummyOrderCreated,
            moment: TestHelper.LowPrecisionNow());
        var fixtureInboundMessage = new InboundMessageEntityFixture(fixtureEvent).Build();

        var result =
            await new EventEnvelopeParser<InboundMessageEntity, PersistedEvent>().FromInboundMessage(fixtureInboundMessage);

        result.Should().BeEquivalentTo(fixtureEvent);
    }

    [Fact]
    public async Task FromInboundMessage_WhenMessageIsJsonNull_ThrowsException()
    {
        var fixtureInboundMessage = new InboundMessageEntity("id", "receipt", "null", "source");

        var result = async () =>
            await new EventEnvelopeParser<InboundMessageEntity, Event>().FromInboundMessage(fixtureInboundMessage);

        await result.Should().ThrowAsync<MessageBrokerParsingException>();
    }
}
