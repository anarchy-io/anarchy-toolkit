namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when an operation exceeds its allotted time. Wraps <see cref="TimeoutException"/>.
/// </summary>
public class OperationTimeoutException(TimeSpan timeout, Exception? innerException = null)
    : TimeoutException($"Operation exceeded timeout of {timeout}.", innerException)
{
    public TimeSpan Timeout { get; } = timeout;
}
