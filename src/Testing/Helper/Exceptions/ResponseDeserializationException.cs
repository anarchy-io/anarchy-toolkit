using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Testing.Helper.Exceptions;

/// <summary>
/// Thrown when a response body cannot be deserialized into the expected type.
/// </summary>
public class ResponseDeserializationException(string source, Type targetType, string rawBody, Exception innerException)
    : InvalidFormatException(
        $"Failed to deserialize {source} response into {targetType.Name}. Raw body:\n{rawBody}", innerException);
