using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when the callback executed through an <see cref="IStoragePolicy{TStorage}"/> fails. The original failure is
/// the inner exception.
/// </summary>
public class StoragePolicyExecutionException : ExternalDependencyException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePolicyExecutionException"/> class with a custom message.
    /// </summary>
    /// <param name="message">A message describing the failure.</param>
    /// <param name="innerException">The original failure.</param>
    public StoragePolicyExecutionException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePolicyExecutionException"/> class with the default message.
    /// </summary>
    /// <param name="innerException">The original failure.</param>
    public StoragePolicyExecutionException(Exception? innerException) : base(
        "Something went wrong when trying to execute the callback via a Data Storage Policy", innerException)
    {
    }
}
