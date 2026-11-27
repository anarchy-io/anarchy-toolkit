using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Envelopes;

public class EventEnvelopeParserTest
{
    [Fact]
    public async Task FromInboundMessage_ForEvent_WhenPayloadIsInvalid_ThrowsException()
    {
        var fixtureInboundMessage = new InboundMessageFixture().Build();

        var result = async () =>
            await new EventEnvelopeParser<InboundMessage, DomainEventEnvelope>().FromInboundMessage(fixtureInboundMessage);

        await result.Should().ThrowAsync<MessageBrokerParsingException>();
    }

    [Fact]
    public async Task FromInboundMessage_ForEvent_WhenPayloadIsValid_ReturnsValidEvent()
    {
        var fixtureEvent = new DomainEventEnvelopeFixture().Build();
        var fixtureInboundMessage = new InboundMessageFixture(fixtureEvent).Build();

        var result =
            await new EventEnvelopeParser<InboundMessage, DomainEventEnvelope>().FromInboundMessage(fixtureInboundMessage);

        var payload = ((JObject)result.Data!).ToObject<DummyEventPayload>();

        result.Should().BeEquivalentTo(fixtureEvent, options => options.Excluding(x => x.Data));
        payload.Should().BeEquivalentTo(fixtureEvent.Data, options =>
            options.Using<DateTimeOffset>(assertionContext =>
                    assertionContext.Subject.Should().BeCloseTo(assertionContext.Expectation, TimeSpan.FromMilliseconds(10)))
                .When(info => info.Path.EndsWith("LastModificationTime")));
    }

    [Fact]
    public async Task FromInboundMessage_WithPersistedDomainEvent_WhenPayloadIsInvalid_ThrowsException()
    {
        var fixtureInboundMessage = new InboundMessageFixture().Build();

        var result = async () =>
            await new EventEnvelopeParser<InboundMessage, PersistedDomainEvent>().FromInboundMessage(fixtureInboundMessage);

        await result.Should().ThrowAsync<MessageBrokerParsingException>();
    }

    [Fact]
    public async Task FromInboundMessage_WithPersistedDomainEvent_WhenPayloadIsValid_ReturnsValidPersistedDomainEvent()
    {
        var fixtureEvent = PersistedDomainEvent.Create(
            TestHelper.RandomId<long>(),
            DummyTypeAggregate.DummyTask,
            TestHelper.RandomId("Test.AggregateId"),
            DummyTypeEvent.DummyOrderCreated,
            time: TestHelper.LowPrecisionNow());
        var fixtureInboundMessage = new InboundMessageFixture(fixtureEvent).Build();

        var result =
            await new EventEnvelopeParser<InboundMessage, PersistedDomainEvent>().FromInboundMessage(fixtureInboundMessage);

        result.Should().BeEquivalentTo(fixtureEvent);
    }

    [Fact]
    public async Task FromInboundMessage_WhenMessageIsJsonNull_ThrowsException()
    {
        var fixtureInboundMessage = new InboundMessage("id", "receipt", "null", "source");

        var result = async () =>
            await new EventEnvelopeParser<InboundMessage, DomainEventEnvelope>().FromInboundMessage(fixtureInboundMessage);

        await result.Should().ThrowAsync<MessageBrokerParsingException>();
    }
}
