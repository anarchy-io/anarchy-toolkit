using System.ComponentModel;

namespace Anarchy.Toolkit.Tests.Unit.Core.Messaging.Abstractions.Dummy;

public enum DummyMessageBrokerDestination
{
    [Description("dummy-destination-one")]
    DestinationOne,

    [Description("dummy-destination-two")]
    DestinationTwo,

    [Description("dummy-destination-three")]
    DestinationThree
}
