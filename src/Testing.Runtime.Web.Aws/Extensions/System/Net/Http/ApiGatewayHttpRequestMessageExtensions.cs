using Amazon.Lambda.APIGatewayEvents;
using Microsoft.AspNetCore.WebUtilities;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Extensions.System.Net.Http;

/// <summary>
/// Extensions on <see cref="HttpRequestMessage"/> that translate test requests into Lambda
/// <see cref="APIGatewayProxyRequest"/> events.
/// </summary>
public static class ApiGatewayHttpRequestMessageExtensions
{
    /// <param name="request">The request being extended.</param>
    extension(HttpRequestMessage request)
    {
        /// <summary>
        /// Converts the request into an <see cref="APIGatewayProxyRequest"/> suitable for invoking a Lambda handler directly.
        /// </summary>
        /// <remarks>
        /// Relative request URIs are made absolute against <c>http://localhost/</c> (the request's
        /// <see cref="HttpRequestMessage.RequestUri"/> is mutated). Query parameters and headers (including content headers)
        /// with multiple values are joined with commas. The body is read as a string and
        /// <see cref="APIGatewayProxyRequest.IsBase64Encoded"/> is always <see langword="false"/>.
        /// The <see cref="HttpRequestMessage.RequestUri"/> must not be <see langword="null"/>.
        /// </remarks>
        /// <param name="cancellationToken">Token used to cancel reading the request content.</param>
        /// <returns>The equivalent API Gateway proxy request.</returns>
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
    }
}
