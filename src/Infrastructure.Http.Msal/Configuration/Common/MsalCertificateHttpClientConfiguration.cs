using Anarchy.Toolkit.Infrastructure.Http.Configuration;
using Anarchy.Toolkit.Infrastructure.Http.Configuration.Common;
using Anarchy.Toolkit.Infrastructure.Http.Msal.Configuration;
using Anarchy.Toolkit.Infrastructure.Http.Msal.Service.Http.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Http.Msal.Configuration.Common;

/// <summary>
/// HTTP client configuration that, in addition to the common settings, authenticates every request with an OAuth 2.0
/// bearer token obtained via MSAL using certificate-based client credentials.
/// </summary>
/// <remarks>
/// Adds a <see cref="MsalCertificateAuthenticationHandler"/> after the handlers configured by
/// <see cref="HttpClientConfigurationBase{TClient}"/>.
/// </remarks>
/// <typeparam name="TClient">Marker type identifying the client/adapter the settings belong to.</typeparam>
/// <param name="settings">The adapter settings, including the MSAL client id, authority, scope and certificate.</param>
public class MsalCertificateHttpClientConfiguration<TClient>(IMsalCertificateHttpAdapterSettings<TClient> settings)
    : HttpClientConfigurationBase<TClient>(settings)
{
    /// <inheritdoc/>
    /// <returns><see cref="IHttpAdapterSettings{TClient}.ClientName"/> from the settings.</returns>
    public override string GetClientName()
    {
        return settings.ClientName;
    }

    /// <inheritdoc/>
    public override IHttpClientBuilder Configure(IHttpClientBuilder builder)
    {
        return base
            .Configure(builder)
            .AddHttpMessageHandler(p => new MsalCertificateAuthenticationHandler(
                settings.ClientId,
                settings.Authority,
                settings.Scope,
                settings.PublicCertificate,
                settings.PrivateKey)
            );
    }
}
