using Newtonsoft.Json;
using System.Net;

namespace Anarchy.Toolkit.Infrastructure.Http.Extensions.System.Net.Http;

/// <summary>
/// Convenience extensions for <see cref="HttpResponseMessage"/>: Newtonsoft.Json deserialization and status checks
/// with custom exceptions.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <param name="this">The HTTP response.</param>
    extension(HttpResponseMessage @this)
    {
        /// <summary>
        /// Shortcut to read content as a type
        /// </summary>
        /// <remarks>
        /// Reads the body as a string and deserializes it with <see cref="JsonConvert"/> default settings. The status
        /// code is not checked.
        /// </remarks>
        /// <typeparam name="TApiModel">The type to deserialize the body into.</typeparam>
        /// <param name="cancellationToken">Token to cancel reading the content.</param>
        /// <returns>The deserialized model, or <see langword="null"/> when the body deserializes to null (e.g. empty).</returns>
        /// <exception cref="JsonException">The body is not valid JSON for <typeparamref name="TApiModel"/>.</exception>
        public async Task<TApiModel?> ReadContentAsync<TApiModel>(CancellationToken cancellationToken = default)
        {
            var content = await @this.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<TApiModel>(content);
        }

        /// <summary>
        /// Shortcut to read content as a type with the option to pass a serializer settings
        /// </summary>
        /// <typeparam name="TApiModel">The type to deserialize the body into.</typeparam>
        /// <param name="settings">The serializer settings to use.</param>
        /// <param name="cancellationToken">Token to cancel reading the content.</param>
        /// <returns>The deserialized model, or <see langword="null"/> when the body deserializes to null (e.g. empty).</returns>
        /// <exception cref="JsonException">The body is not valid JSON for <typeparamref name="TApiModel"/>.</exception>
        public async Task<TApiModel?> ReadContentAsync<TApiModel>(JsonSerializerSettings settings,
                                                                  CancellationToken cancellationToken = default)
        {
            var content = await @this.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<TApiModel>(content, settings);
        }

        /// <summary>
        /// Provides custom exception for non success status code failures
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="HttpResponseMessage.EnsureSuccessStatusCode()"/>, the exception thrown is the one built by
        /// <paramref name="errorFunc"/>. Success is determined by <see cref="HttpResponseMessage.IsSuccessStatusCode"/>
        /// (2xx).
        /// </remarks>
        /// <param name="errorFunc">Factory that builds the exception to throw from the response status code.</param>
        /// <returns>The same response, to allow chaining, when the status code is successful.</returns>
        /// <exception cref="Exception">The exception returned by <paramref name="errorFunc"/> when the status code is not successful.</exception>
        public HttpResponseMessage EnsureSuccessStatusCode(Func<HttpStatusCode, Exception> errorFunc)
        {
            if (@this.IsSuccessStatusCode)
                return @this;

            throw errorFunc(@this.StatusCode);
        }
    }
}
