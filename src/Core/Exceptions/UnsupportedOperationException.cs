namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for operations that are intentionally not supported. Wraps <see cref="NotSupportedException"/>.
/// </summary>
public class UnsupportedOperationException(string message) : NotSupportedException(message);
