namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when the JSON input ends before the target type has been fully read.
/// </summary>
public class UnexpectedJsonEndException(Type targetType)
    : JsonParsingException($"Unexpected end of JSON when parsing {targetType.Name}.");
