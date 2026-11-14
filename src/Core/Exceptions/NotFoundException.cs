namespace Anarchy.Toolkit.Core.Exceptions;

public class NotFoundException : Exception
{
    public readonly object? Filters;
    public readonly object? Object;

    public NotFoundException() : base("The item was not found")
    {
    }

    public NotFoundException(object obj) : base($"The item {obj} not found")
    {
        Object = obj;
    }

    public NotFoundException(string type, object filters, Exception? innerException = null)
        : base($"No item of type {type} found based on filters", innerException)
    {
        Filters = filters;
    }

    public NotFoundException(string message) : base(message)
    {
    }

    protected NotFoundException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
