using System.Text.Json;

namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for JSON (System.Text.Json) parsing failures. Wraps <see cref="JsonException"/> so the serializer can still
/// enrich it with path information.
/// </summary>
public class JsonParsingException(string message, Exception? innerException = null)
    : JsonException(message, innerException);
