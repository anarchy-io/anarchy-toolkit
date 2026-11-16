namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when an operation exceeds its allotted time. Wraps <see cref="TimeoutException"/>.
/// </summary>
/// <param name="timeout">The timeout that was exceeded.</param>
/// <param name="innerException">The exception that caused this one, if any.</param>
public class OperationTimeoutException(TimeSpan timeout, Exception? innerException = null)
    : TimeoutException($"Operation exceeded timeout of {timeout}.", innerException)
{
    /// <summary>
    /// Gets the timeout that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; } = timeout;
}
