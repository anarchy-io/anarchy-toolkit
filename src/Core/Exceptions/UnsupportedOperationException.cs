namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for operations that are intentionally not supported. Wraps <see cref="NotSupportedException"/>.
/// </summary>
/// <param name="message">A message describing the unsupported operation.</param>
public class UnsupportedOperationException(string message) : NotSupportedException(message);
