using System.ComponentModel;

namespace Anarchy.Toolkit.Tests.Unit.Core.Messaging.Abstractions.Dummy;

public enum DummyMessageBrokerSource
{
    [Description("dummy-source-one")]
    SourceOne,

    [Description("dummy-source-two")]
    SourceTwo
}
