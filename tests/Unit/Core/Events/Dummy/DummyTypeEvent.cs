using System.ComponentModel;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

public enum DummyTypeEvent
{
    [Description("dummy_unknown")]
    Unknown,

    [Description("dummy_order_created")]
    DummyOrderCreated,

    [Description("dummy_order_ready")]
    DummyOrderReady,

    [Description("dummy_order_cancelled")]
    DummyOrderCancelled
}
