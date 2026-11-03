using Anarchy.Toolkit.Testing.Helper.Exceptions;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Testing.Helper;

public static class ResponseHelper
{
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
