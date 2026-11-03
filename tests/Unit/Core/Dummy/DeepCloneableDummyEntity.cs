using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Tests.Unit.Core.Dummy;

public sealed class DeepCloneableDummyEntity : IEntity, IDeepCloneableEntity<DeepCloneableDummyEntity>
{
    public string Name { get; init; } = Guid.NewGuid().ToString();
    public List<string> Values { get; init; } = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()];

    public DeepCloneableDummyEntity DeepClone()
    {
        return new DeepCloneableDummyEntity { Name = Name, Values = [.. Values] };
    }
}
