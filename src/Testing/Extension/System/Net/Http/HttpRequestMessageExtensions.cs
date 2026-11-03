using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Helper;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Anarchy.Toolkit.Testing.Extension.System.Net.Http;

public static class HttpRequestMessageExtensions
{
    extension(HttpRequestMessage request)
    {
        public HttpRequestMessage WithJsonPayload(object payload)
        {
            request.Content = RequestHelper.CreateJsonContent(payload);
            return request;
        }

        public async Task<APIGatewayProxyRequest> ToApiGatewayProxyRequestAsync(
            CancellationToken cancellationToken = default)
        {
            if (!request.RequestUri!.IsAbsoluteUri)
            {
                // Ensure absolute URI for Lambda request conversion
                // Otherwise, APIGatewayProxyRequest conversion may fail
                // to parse query and header parameters or calculating absolute path below
                request.RequestUri =
                    new Uri("http://localhost/" + request.RequestUri.ToString()
                        .TrimStart('/'), UriKind.Absolute);
            }

            // Parse query string using QueryHelpers
            var queryDict = request.RequestUri != null ?
                QueryHelpers.ParseQuery(request.RequestUri.Query)
                    .ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value.ToArray())) :
                null;

            // Merge headers + content headers
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            if (request.Content?.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(",", header.Value);
                }
            }

            var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;

            return new APIGatewayProxyRequest {
                HttpMethod = request.Method.Method,
                Path = request.RequestUri?.AbsolutePath,
                QueryStringParameters = queryDict,
                Headers = headers,
                Body = body,
                IsBase64Encoded = false
            };
        }

        public HttpRequestMessage Clone(string? content)
        {
            var httpRequestMessage = new HttpRequestMessage(request.Method, request.RequestUri) {
                Version = request.Version
            };

            if (content is not null)
            {
                httpRequestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            foreach (var header in request.Headers)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in request.Options)
            {
                httpRequestMessage.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }

            return httpRequestMessage;
        }
    }
}
