using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Testing.Exceptions;

/// <summary>
/// Thrown when a response body cannot be deserialized into the expected type.
/// </summary>
/// <param name="source">A label describing where the response came from (e.g. <c>native</c> or <c>lambda</c>).</param>
/// <param name="targetType">The type the body was being deserialized into.</param>
/// <param name="rawBody">The raw body, included in the message.</param>
/// <param name="innerException">The underlying deserialization error.</param>
public class ResponseDeserializationException(string source, Type targetType, string rawBody, Exception innerException)
    : InvalidFormatException(
        $"Failed to deserialize {source} response into {targetType.Name}. Raw body:\n{rawBody}", innerException);
