using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Testing.Fixture;
using Anarchy.Toolkit.Testing.Helper;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

public class InboundMessageFixture(object? messagePayload = null) : EntityFixtureBase<InboundMessage>
{
    protected override InboundMessage BuildDefaultFixture()
    {
        return new InboundMessage(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            messagePayload?.ToJson() ?? TestHelper.RandomId("Test.Payload"),
            TestHelper.RandomId("Test.Source"),
            TestHelper.RandomId("Test.AggregateId")
        );
    }
}
