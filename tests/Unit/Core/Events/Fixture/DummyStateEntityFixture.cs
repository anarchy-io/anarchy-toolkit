using Anarchy.Toolkit.Testing.Fixture;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

public class DummyStateEntityFixture : EntityFixtureBase<DummyStateEntity>
{
    protected override DummyStateEntity BuildDefaultFixture()
    {
        return new DummyStateEntity {
            Id = TestHelper.RandomId<long>(),
            AggregateId = TestHelper.RandomId("Test.AggregateId"),
            Status = TestHelper.RandomId("Test.Status")
        };
    }
}
