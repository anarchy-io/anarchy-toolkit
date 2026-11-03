namespace Anarchy.Toolkit.Core.Entity;

/// <summary>
/// Contract for entities that can produce a deep copy of themselves.
/// </summary>
public interface IDeepCloneableEntity<out T>
{
    T DeepClone();
}
