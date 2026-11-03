using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Configuration.Common;

public interface IHttpClientConfiguration
{
    string GetClientName();

    IHttpClientBuilder Configure(IHttpClientBuilder builder);
}

public abstract class HttpClientConfigurationBase<TClient>(IHttpAdapterSettings<TClient> settings)
    : IHttpClientConfiguration
{
    public abstract string GetClientName();

    public virtual IHttpClientBuilder Configure(IHttpClientBuilder builder)
    {
        ValidateSettings();

        return builder
            .ConfigureHttpClient((_, client) => {
                client.Timeout = TimeSpan.FromMilliseconds(settings.DefaultTimeoutMSec);
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler(_ => new HttpTimeoutHandler());
    }

    private void ValidateSettings()
    {
        var msg = $"HttpClient {GetClientName()} configuration is invalid";

        settings.BaseUrl.CheckNotEmpty(nameof(settings.BaseUrl), msg);
        (settings.DefaultTimeoutMSec > 0).CheckTrue(nameof(settings.DefaultTimeoutMSec), msg);
    }
}
