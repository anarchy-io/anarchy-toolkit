using System.Net;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler;

public class HttpTimeoutHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new HttpResponseMessage {
                StatusCode = HttpStatusCode.GatewayTimeout,
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"))
            };
        }
    }
}
