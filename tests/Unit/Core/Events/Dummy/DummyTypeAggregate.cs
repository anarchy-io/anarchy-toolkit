using System.ComponentModel;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

public enum DummyTypeAggregate
{
    [Description("dummy_unknown")]
    Unknown,

    [Description("dummy_order")]
    DummyOrder,

    [Description("dummy_task")]
    DummyTask
}
