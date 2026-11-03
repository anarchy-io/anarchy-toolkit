using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.Helper.Assertions.FailureOutput.ApiGatewayResponse;

public static class ApiGatewayResponsePlainFailureOutput
{
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
