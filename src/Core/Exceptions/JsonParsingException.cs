using System.Text.Json;

namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for JSON (System.Text.Json) parsing failures. Wraps <see cref="JsonException"/> so the serializer can still
/// enrich it with path information.
/// </summary>
/// <param name="message">A message describing the parsing failure.</param>
/// <param name="innerException">The exception that caused this one, if any.</param>
public class JsonParsingException(string message, Exception? innerException = null)
    : JsonException(message, innerException);
