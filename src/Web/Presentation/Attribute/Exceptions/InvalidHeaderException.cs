using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Web.Presentation.Attribute.Exceptions;

/// <summary>
/// Thrown when a request header is missing or has an invalid value.
/// </summary>
public class InvalidHeaderException : InvalidArgumentException
{
    /// <summary>Creates the exception with a default message.</summary>
    public InvalidHeaderException() { }
    /// <summary>Creates the exception with the given message.</summary>
    /// <param name="message">The error message.</param>
    public InvalidHeaderException(string message) : base(message) { }
    /// <summary>Creates the exception with the given message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public InvalidHeaderException(string message, Exception innerException) : base(message, innerException) { }
}
