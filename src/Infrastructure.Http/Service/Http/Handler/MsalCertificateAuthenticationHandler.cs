using Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler.Exceptions;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler;

/// <summary>
/// MSAL Authentication Handler using certificate-based OAuth 2.0 client credentials authentication
/// </summary>
public class MsalCertificateAuthenticationHandler : DelegatingHandler
{
    private readonly Lazy<IConfidentialClientApplication> _app;
    private readonly string _scope;

    /// <summary>
    /// MSAL Authentication Handler using certificate-based OAuth 2.0 client credentials authentication
    /// </summary>
    /// <param name="clientId">Client ID of the application</param>
    /// <param name="authority">Authority URL (e.g., https://login.microsoftonline.com/{tenant})</param>
    /// <param name="scope">Scope for which the access token is requested</param>
    /// <param name="publicCertificate">Public certificate in PEM format</param>
    /// <param name="privateKey">Private key for the certificate</param>
    public MsalCertificateAuthenticationHandler(string clientId,
                                                string authority,
                                                string scope,
                                                string publicCertificate,
                                                string privateKey)
    {
        _scope = scope;
        _app = new Lazy<IConfidentialClientApplication>(() => ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(authority)
            .WithCertificate(LoadCertificate(publicCertificate, privateKey))
            .Build()
        );
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private static X509Certificate2 LoadCertificate(string publicCertificate, string privateKey)
    {
        var cert = X509Certificate2.CreateFromPem(publicCertificate, privateKey);

        if (!cert.HasPrivateKey)
        {
            throw new CertificatePrivateKeyMissingException();
        }

        return cert;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _app.Value.AcquireTokenForClient([_scope])
                .ExecuteAsync(cancellationToken);

            return result.AccessToken;
        }
        catch (MsalException ex)
        {
            throw new AccessTokenAcquisitionException(ex);
        }
    }
}
