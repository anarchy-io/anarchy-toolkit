using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Extension.Exceptions;

/// <summary>
/// Thrown by the <c>CheckTrue</c> guard when its condition does not hold.
/// </summary>
public class ArgumentCheckException(string paramName, string? message = null)
    : InvalidArgumentException(string.IsNullOrEmpty(message) ? "Argument is invalid" : message, paramName);
