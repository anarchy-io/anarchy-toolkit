using Anarchy.Toolkit.Infrastructure.Http.Configuration;

namespace Anarchy.Toolkit.Infrastructure.Http.Msal.Configuration;

/// <summary>
/// Settings for an HTTP adapter authenticated via MSAL certificate-based client credentials.
/// </summary>
/// <remarks>
/// Used by
/// <see cref="Anarchy.Toolkit.Infrastructure.Http.Msal.Configuration.Common.MsalCertificateHttpClientConfiguration{TClient}"/>.
/// </remarks>
/// <typeparam name="TClient">Marker type identifying the adapter these settings belong to.</typeparam>
public interface IMsalCertificateHttpAdapterSettings<TClient> : IHttpAdapterSettings<TClient>
{
    /// <summary>Gets the client (application) id.</summary>
    string ClientId { get; }

    /// <summary>Gets the authority URL, e.g. <c>https://login.microsoftonline.com/{tenant}</c>.</summary>
    string Authority { get; }

    /// <summary>Gets the scope the access token is requested for.</summary>
    string Scope { get; }

    /// <summary>Gets the public certificate in PEM format.</summary>
    string PublicCertificate { get; }

    /// <summary>Gets the PEM-encoded private key matching <see cref="PublicCertificate"/>.</summary>
    string PrivateKey { get; }
}
