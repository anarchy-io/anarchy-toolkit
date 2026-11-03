using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Configuration;

public interface IHttpAdapterSettings<[SuppressMessage("ReSharper", "UnusedTypeParameter")] TClient>
{
    string BaseUrl { get; }
    int DefaultTimeoutMSec { get; }
    string ClientName { get; }
}

public interface IApiKeyHttpAdapterSettings<TClient> : IHttpAdapterSettings<TClient>
{
    string ConsumerApiKey { get; }
    ApiKeyType ConsumerApiKeyType { get; }
}

public interface IAuthServiceHttpAdapterSettings<TClient> : IHttpAdapterSettings<TClient>
{
    string BaseUrlAuthService { get; }
    string AuthServiceClientId { get; }
    string EncryptedOutboundClientSecretAuthService { get; }
}

public interface IMsalCertificateHttpAdapterSettings<TClient> : IHttpAdapterSettings<TClient>
{
    string ClientId { get; }
    string Authority { get; }
    string Scope { get; }
    string PublicCertificate { get; }
    string PrivateKey { get; }
}

public enum ApiKeyType
{
    None,
    ApiKey,
    XApiKey,
    Bearer
}
