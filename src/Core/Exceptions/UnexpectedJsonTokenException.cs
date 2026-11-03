using System.Text.Json;

namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when the JSON reader is positioned on a token that cannot be converted into the target type.
/// </summary>
public class UnexpectedJsonTokenException(JsonTokenType token, Type targetType)
    : JsonParsingException($"Unexpected token {token} when parsing {targetType.Name}.")
{
    public JsonTokenType Token { get; } = token;
}
