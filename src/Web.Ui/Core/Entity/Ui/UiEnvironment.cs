namespace Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;

/// <summary>
/// An environment a UI session can target: the auth service that issues its tokens and the base URLs of the
/// downstream APIs the UI talks to, keyed by a consumer-chosen API name.
/// </summary>
/// <param name="Name">Environment name (e.g. "Testing"); matched case-insensitively.</param>
/// <param name="AuthServiceBaseUrl">Base URL of the OAuth server (the token endpoint is <c>connect/token</c>).</param>
/// <param name="ApiBaseUrls">Downstream API base URLs, keyed by API name.</param>
public sealed record UiEnvironment(string Name, Uri AuthServiceBaseUrl, IReadOnlyDictionary<string, Uri> ApiBaseUrls)
{
    public UiEnvironment(string name, Uri authServiceBaseUrl)
        : this(name, authServiceBaseUrl, new Dictionary<string, Uri>())
    {
    }

    /// <summary>
    /// Returns the base URL of the given downstream API (matched case-insensitively).
    /// </summary>
    /// <exception cref="KeyNotFoundException">When the API is not configured for this environment.</exception>
    public Uri GetApiBaseUrl(string apiName)
    {
        if (ApiBaseUrls.TryGetValue(apiName, out var url))
        {
            return url;
        }

        foreach (var (name, value) in ApiBaseUrls)
        {
            if (string.Equals(name, apiName, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        throw new KeyNotFoundException($"API '{apiName}' is not configured for environment '{Name}'");
    }
}
