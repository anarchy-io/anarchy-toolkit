using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Ui.Entity.Api.Output;

/// <summary>
/// Response containing authentication token information.
/// </summary>
public record AuthTokenResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    [JsonPropertyName("token_type")] public required string TokenType { get; init; }
    [JsonPropertyName("expires_in")] public required int ExpiresIn { get; init; }
}
