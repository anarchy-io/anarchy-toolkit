using System.Net;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler;

/// <summary>
/// Delegating handler that converts cancellations/timeouts into a synthetic <see cref="HttpStatusCode.GatewayTimeout"/>
/// (504) response with an empty JSON object (<c>{}</c>) body instead of throwing.
/// </summary>
/// <remarks>
/// Added automatically by
/// <see cref="Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Configuration.Common.HttpClientConfigurationBase{TClient}"/>.
/// Note that any <see cref="OperationCanceledException"/> raised by inner handlers is converted, including one caused
/// by the caller's own cancellation token.
/// </remarks>
public class HttpTimeoutHandler : DelegatingHandler
{
    /// <inheritdoc/>
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
