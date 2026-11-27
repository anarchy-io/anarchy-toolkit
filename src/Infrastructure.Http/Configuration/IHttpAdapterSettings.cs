using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Infrastructure.Http.Configuration;

/// <summary>
/// Common settings for an outbound HTTP adapter.
/// </summary>
/// <remarks>
/// The generic parameter is only a marker that lets several settings implementations coexist in the DI container, one
/// per adapter. Consumed by
/// <see cref="Anarchy.Toolkit.Infrastructure.Http.Configuration.Common.HttpClientConfigurationBase{TClient}"/>.
/// </remarks>
/// <typeparam name="TClient">Marker type identifying the adapter these settings belong to.</typeparam>
public interface IHttpAdapterSettings<[SuppressMessage("ReSharper", "UnusedTypeParameter")] TClient>
{
    /// <summary>Gets the base URL of the remote API; must not be empty.</summary>
    string BaseUrl { get; }

    /// <summary>Gets the client timeout in milliseconds; must be greater than zero.</summary>
    int DefaultTimeoutMSec { get; }

    /// <summary>Gets the name of the client registered with <see cref="IHttpClientFactory"/>.</summary>
    string ClientName { get; }
}

/// <summary>
/// Settings for an HTTP adapter that authenticates with an API key.
/// </summary>
/// <typeparam name="TClient">Marker type identifying the adapter these settings belong to.</typeparam>
public interface IApiKeyHttpAdapterSettings<TClient> : IHttpAdapterSettings<TClient>
{
    /// <summary>Gets the API key sent to the remote API.</summary>
    string ConsumerApiKey { get; }

    /// <summary>Gets how the API key is sent.</summary>
    ApiKeyType ConsumerApiKeyType { get; }
}

/// <summary>
/// Settings for an HTTP adapter that obtains credentials from an authentication service.
/// </summary>
/// <typeparam name="TClient">Marker type identifying the adapter these settings belong to.</typeparam>
public interface IAuthServiceHttpAdapterSettings<TClient> : IHttpAdapterSettings<TClient>
{
    /// <summary>Gets the base URL of the authentication service.</summary>
    string BaseUrlAuthService { get; }

    /// <summary>Gets the client id registered with the authentication service.</summary>
    string AuthServiceClientId { get; }

    /// <summary>Gets the encrypted client secret used to authenticate with the authentication service.</summary>
    string EncryptedOutboundClientSecretAuthService { get; }
}

/// <summary>
/// How an API key is sent to a remote API. The toolkit does not interpret this value itself; adapters consuming
/// <see cref="IApiKeyHttpAdapterSettings{TClient}"/> decide how to apply it.
/// </summary>
public enum ApiKeyType
{
    /// <summary>No API key is sent.</summary>
    None,

    /// <summary>API key sent in an API-key style header.</summary>
    ApiKey,

    /// <summary>API key sent in an <c>X-Api-Key</c> style header.</summary>
    XApiKey,

    /// <summary>API key sent as a bearer token.</summary>
    Bearer
}
