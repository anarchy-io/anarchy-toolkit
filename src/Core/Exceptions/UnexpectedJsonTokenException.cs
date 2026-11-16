using System.Text.Json;

namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when the JSON reader is positioned on a token that cannot be converted into the target type.
/// </summary>
/// <param name="token">The unexpected token type.</param>
/// <param name="targetType">The type being parsed.</param>
public class UnexpectedJsonTokenException(JsonTokenType token, Type targetType)
    : JsonParsingException($"Unexpected token {token} when parsing {targetType.Name}.")
{
    /// <summary>
    /// Gets the token type the reader was positioned on.
    /// </summary>
    public JsonTokenType Token { get; } = token;
}
