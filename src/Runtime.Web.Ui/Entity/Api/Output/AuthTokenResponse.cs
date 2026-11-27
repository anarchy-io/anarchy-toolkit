using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Runtime.Web.Ui.Entity.Api.Output;

/// <summary>
/// Response containing authentication token information.
/// </summary>
/// <remarks>Deserialized from the OAuth <c>connect/token</c> endpoint response by <c>UiAuthHandler</c>.</remarks>
public record AuthTokenResponse
{
    /// <summary>The access token (<c>access_token</c>).</summary>
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    /// <summary>The token type (<c>token_type</c>), typically <c>Bearer</c>.</summary>
    [JsonPropertyName("token_type")] public required string TokenType { get; init; }
    /// <summary>The token lifetime in seconds (<c>expires_in</c>); also used as the session cookie lifetime.</summary>
    [JsonPropertyName("expires_in")] public required int ExpiresIn { get; init; }
}
