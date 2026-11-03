namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

public class EmptyAsyncContextException(Exception? innerException = null)
    : NullReferenceException("No value is present in the current async context.", innerException)
{
}
