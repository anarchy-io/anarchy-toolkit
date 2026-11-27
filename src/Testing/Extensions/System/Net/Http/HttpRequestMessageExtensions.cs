using Anarchy.Toolkit.Testing.Helpers.Http;
using System.Text;

namespace Anarchy.Toolkit.Testing.Extensions.System.Net.Http;

/// <summary>
/// Extensions on <see cref="HttpRequestMessage"/> that help build test requests.
/// </summary>
public static class HttpRequestMessageExtensions
{
    /// <param name="request">The request being extended.</param>
    extension(HttpRequestMessage request)
    {
        /// <summary>
        /// Sets the request content to <paramref name="payload"/> serialized as JSON (UTF-8, <c>application/json</c>)
        /// using <see cref="RequestHelper.CreateJsonContent{T}(T)"/>.
        /// </summary>
        /// <param name="payload">The object to serialize into the request body.</param>
        /// <returns>The same <see cref="HttpRequestMessage"/> instance, for chaining.</returns>
        public HttpRequestMessage WithJsonPayload(object payload)
        {
            request.Content = RequestHelper.CreateJsonContent(payload);
            return request;
        }

        /// <summary>
        /// Creates a copy of the request with the same method, URI, version, headers and options, optionally replacing the body.
        /// </summary>
        /// <param name="content">
        /// The new body sent as UTF-8 <c>application/json</c>, or <see langword="null"/> to create the clone without content.
        /// The original content is never copied.
        /// </param>
        /// <returns>A new <see cref="HttpRequestMessage"/>.</returns>
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
