using Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client;
using System.Net;
using System.Net.Http.Headers;

namespace Anarchy.Toolkit.Web.Ui.Composition.Extension.Microsoft.DependencyInjection;

public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the named HttpClient used by <see cref="UiApiClientBase"/>: a shared timeout for every
        /// downstream API, and transparent gzip/brotli response compression.
        /// </summary>
        /// <param name="timeout">Timeout applied to each downstream call.</param>
        /// <param name="name">HttpClient name; override together with <c>UiApiClientBase.HttpClientName</c>.</param>
        public IHttpClientBuilder AddToolkitUiApiHttpClient(TimeSpan timeout,
                                                            string name = UiApiClientBase.ClientName)
        {
            return services.AddHttpClient(name, client => {
                client.Timeout = timeout;

                // Ask downstream APIs to compress responses; SocketsHttpHandler below
                // transparently decompresses the body before it reaches our client code.
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                });
        }
    }
}
