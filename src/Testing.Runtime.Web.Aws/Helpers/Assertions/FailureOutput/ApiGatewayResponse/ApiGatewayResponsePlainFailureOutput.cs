using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Helpers.Assertions.FailureOutput.ApiGatewayResponse;

/// <summary>
/// Formats an <see cref="APIGatewayProxyResponse"/> as plain text for assertion failure messages.
/// </summary>
public static class ApiGatewayResponsePlainFailureOutput
{
    /// <summary>
    /// Renders the status code, indented JSON headers (or <c>&lt;none&gt;</c>) and the body, pretty-printed when it is
    /// valid JSON (raw otherwise, <c>&lt;null&gt;</c> when empty).
    /// </summary>
    /// <param name="response">The response to format.</param>
    /// <returns>The formatted dump.</returns>
    public static string Format(APIGatewayProxyResponse response)
    {
        return $@"==== API Gateway Response Dump ====
StatusCode: {response.StatusCode}
Headers:
{PrintHeaders(response)}

Body:
{PrettyPrintBody(response)}
===================================";
    }

    private static string PrintHeaders(APIGatewayProxyResponse response)
    {
        return response.Headers is { Count: > 0 } ?
            JsonSerializer.Serialize(response.Headers, new JsonSerializerOptions { WriteIndented = true }) :
            "<none>";
    }

    private static string PrettyPrintBody(APIGatewayProxyResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Body)) return "<null>";

        try
        {
            using var doc = JsonDocument.Parse(response.Body);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return response.Body;
        }
    }
}
