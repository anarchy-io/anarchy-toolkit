using Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Configuration.Common;

public class MsalCertificateHttpClientConfiguration<TClient>(IMsalCertificateHttpAdapterSettings<TClient> settings)
    : HttpClientConfigurationBase<TClient>(settings)
{
    public override string GetClientName()
    {
        return settings.ClientName;
    }

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
