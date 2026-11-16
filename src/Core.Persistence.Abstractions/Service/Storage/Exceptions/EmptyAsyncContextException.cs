namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when reading <see cref="IAsyncFlowContext{TValue}.Value"/> while no value is set in the current async flow
/// (e.g. using a storage connection outside of a unit-of-work session).
/// </summary>
/// <param name="innerException">The exception that caused this one, if any.</param>
public class EmptyAsyncContextException(Exception? innerException = null)
    : NullReferenceException("No value is present in the current async context.", innerException)
{
}
