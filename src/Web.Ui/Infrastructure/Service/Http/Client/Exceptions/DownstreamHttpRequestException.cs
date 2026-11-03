using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client.Exceptions;

/// <summary>
/// A failed downstream API call, carrying a human-readable copy of the response body so the UI can show it.
/// </summary>
public class DownstreamHttpRequestException : HttpRequestException
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string[] SummaryPropertyNames = ["detail", "message", "title", "error"];

    private DownstreamHttpRequestException(
        string message,
        HttpStatusCode statusCode,
        string responseContent,
        JsonElement? responseJson) : base(message, null, statusCode)
    {
        ResponseContent = responseContent;
        ResponseJson = responseJson;
    }

    /// <summary>
    /// Human-readable response body: indented JSON when the body is valid JSON, the raw body otherwise,
    /// or the status line when the body is empty/unreadable. Never empty.
    /// </summary>
    public string ResponseContent { get; }

    /// <summary>
    /// The parsed response body, when it is valid JSON.
    /// </summary>
    public JsonElement? ResponseJson { get; }

    public static async Task<DownstreamHttpRequestException> CreateAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var statusLine = $"{(int)response.StatusCode} {response.ReasonPhrase ?? response.StatusCode.ToString()}";
        var message = $"Downstream API call to '{response.RequestMessage?.RequestUri}' failed with {statusLine}";

        string rawContent;

        try
        {
            rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            return new DownstreamHttpRequestException(
                message,
                response.StatusCode,
                $"{statusLine} (unable to read response body: {exception.Message})",
                null);
        }

        if (TryParseJson(rawContent) is not { } json)
        {
            var content = string.IsNullOrWhiteSpace(rawContent) ? statusLine : rawContent;
            return new DownstreamHttpRequestException(message, response.StatusCode, content, null);
        }

        var summary = TryGetSummary(json);

        return new DownstreamHttpRequestException(
            summary is null ? message : $"{message}: {summary}",
            response.StatusCode,
            JsonSerializer.Serialize(json, IndentedJsonOptions),
            json);
    }

    private static JsonElement? TryParseJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryGetSummary(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object) return null;

        foreach (var name in SummaryPropertyNames)
        {
            foreach (var property in json.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String
                    && string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                    && property.Value.GetString() is { Length: > 0 } value)
                {
                    return value;
                }
            }
        }

        return null;
    }
}
