using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

public class DsPolicyExecutionException : ExternalDependencyException
{
    public DsPolicyExecutionException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public DsPolicyExecutionException(Exception? innerException) : base(
        "Something went wrong when trying to execute the callback via a Data Storage Policy", innerException)
    {
    }
}
