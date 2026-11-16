using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Extension.Exceptions;

/// <summary>
/// Thrown by the <see cref="EmptyGuardExtension.CheckTrue(bool, string, string?)"/> guard when its condition does
/// not hold.
/// </summary>
/// <param name="paramName">The name of the argument that failed the check.</param>
/// <param name="message">Optional custom message; defaults to <c>"Argument is invalid"</c> when null or empty.</param>
public class ArgumentCheckException(string paramName, string? message = null)
    : InvalidArgumentException(string.IsNullOrEmpty(message) ? "Argument is invalid" : message, paramName);
