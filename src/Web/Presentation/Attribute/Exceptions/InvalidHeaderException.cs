using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Web.Presentation.Attribute.Exceptions;

/// <summary>
/// Thrown when a request header is missing or has an invalid value.
/// </summary>
public class InvalidHeaderException : InvalidArgumentException
{
    public InvalidHeaderException() { }
    public InvalidHeaderException(string message) : base(message) { }
    public InvalidHeaderException(string message, Exception innerException) : base(message, innerException) { }
}
