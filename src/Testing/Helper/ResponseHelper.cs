using Anarchy.Toolkit.Testing.Helper.Exceptions;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Testing.Helper;

/// <summary>
/// Helpers for deserializing HTTP and Lambda response bodies in tests, using Newtonsoft.Json.
/// </summary>
public static class ResponseHelper
{
    /// <summary>
    /// Reads the response content and deserializes it into <typeparamref name="TParsedOutput"/>.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected body type.</typeparam>
    /// <param name="response">The HTTP response to read.</param>
    /// <param name="cancellationToken">Token used to cancel reading the content.</param>
    /// <returns>The deserialized body, or <see langword="null"/> when the body is empty.</returns>
    /// <exception cref="ResponseDeserializationException">The body is not valid JSON for <typeparamref name="TParsedOutput"/>.</exception>
    public static async Task<TParsedOutput?> ParseHttpResponse<TParsedOutput>(HttpResponseMessage response,
                                                                             CancellationToken cancellationToken = default)
        where TParsedOutput : class
    {
        var parsedBody = default(TParsedOutput);

        var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrEmpty(contentString))
        {
            parsedBody = ParseJsonBody<TParsedOutput>(contentString, "native");
        }

        return parsedBody;
    }

    /// <summary>
    /// Deserializes <paramref name="rawBody"/> into <typeparamref name="TParsedOutput"/>.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected body type.</typeparam>
    /// <param name="rawBody">The raw JSON body.</param>
    /// <param name="source">A label for the response origin, used in the error message.</param>
    /// <returns>The deserialized body, or <see langword="null"/> when <paramref name="rawBody"/> is empty.</returns>
    /// <exception cref="ResponseDeserializationException">The body is not valid JSON for <typeparamref name="TParsedOutput"/>.</exception>
    public static TParsedOutput? ParseJsonBody<TParsedOutput>(string rawBody, string source)
        where TParsedOutput : class
    {
        try
        {
            return string.IsNullOrEmpty(rawBody) ? null : JsonConvert.DeserializeObject<TParsedOutput>(rawBody);
        }
        catch (JsonException ex)
        {
            throw new ResponseDeserializationException(source, typeof(TParsedOutput), rawBody, ex);
        }
    }
}
