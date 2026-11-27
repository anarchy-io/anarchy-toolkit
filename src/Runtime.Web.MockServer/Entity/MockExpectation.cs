using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Runtime.Web.MockServer.Entity;

/// <summary>
/// A mocked endpoint: when a request matches <see cref="HttpRequest"/>, <see cref="HttpResponse"/> is answered. The
/// JSON shape follows <see href="https://www.mock-server.com/">MockServer</see>'s expectations, so existing
/// <c>expectations/*.json</c> files can be reused.
/// </summary>
/// <example>
/// <code language="json">
/// [
///   {
///     "httpRequest": { "method": "GET", "path": "/orders/{id}", "queryStringParameters": { "expand": ["lines"] } },
///     "httpResponse": { "statusCode": 200, "headers": { "Content-Type": ["application/json"] }, "body": { "id": 1 } }
///   }
/// ]
/// </code>
/// </example>
public sealed class MockExpectation
{
    /// <summary>An optional identifier, used in the console output and to remove it at runtime.</summary>
    public string? Id { get; set; }

    /// <summary>What a request must look like to be answered by this expectation.</summary>
    public MockRequestMatcher HttpRequest { get; set; } = new();

    /// <summary>The answer.</summary>
    public MockResponse HttpResponse { get; set; } = new();

    /// <summary>Higher priorities are matched first; equal priorities prefer the most specific path.</summary>
    public int Priority { get; set; }

    /// <summary>How many times it may answer; unlimited when absent.</summary>
    public MockTimes? Times { get; set; }

    /// <summary>Where it was loaded from (a file and index, or <c>runtime</c>/<c>code</c>).</summary>
    [JsonIgnore]
    public string Source { get; set; } = "code";

    /// <summary>A short description for logs: its id or source, and its method and path.</summary>
    [JsonIgnore]
    public string DisplayName => $"{Id ?? Source} ({HttpRequest.Method ?? "*"} {HttpRequest.Path ?? "*"})";
}

/// <summary>
/// The request side of a <see cref="MockExpectation"/>. Every property that is set must match.
/// </summary>
public sealed class MockRequestMatcher
{
    /// <summary>The HTTP method, case-insensitive; any method when absent.</summary>
    public string? Method { get; set; }

    /// <summary>
    /// The path: exact (<c>/orders</c>), templated (<c>/orders/{id}</c>, one segment per parameter) or a regular
    /// expression matching the whole path (<c>/orders/[0-9]+</c>). Any path when absent.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Required query parameters, as a name to a value or a list of values (each listed value must be present,
    /// matched as a regular expression).
    /// </summary>
    public Dictionary<string, JsonElement>? QueryStringParameters { get; set; }

    /// <summary>Required headers, case-insensitive names, with the same value rules as <see cref="QueryStringParameters"/>.</summary>
    public Dictionary<string, JsonElement>? Headers { get; set; }

    /// <summary>
    /// The body: a JSON object or array the request body must contain (extra properties are ignored), a string it must
    /// equal, or a MockServer matcher such as <c>{ "type": "JSON", "json": … }</c>, <c>{ "type": "STRING", "string": …,
    /// "subString": true }</c> or <c>{ "type": "REGEX", "regex": … }</c>.
    /// </summary>
    public JsonElement? Body { get; set; }
}

/// <summary>
/// The response side of a <see cref="MockExpectation"/>.
/// </summary>
public sealed class MockResponse
{
    /// <summary>The status code; <c>200</c> by default.</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>Response headers, as a name to a value or a list of values.</summary>
    public Dictionary<string, JsonElement>? Headers { get; set; }

    /// <summary>
    /// The body: a string is written as-is, anything else as JSON (with <c>Content-Type: application/json</c> unless a
    /// header says otherwise). MockServer's <c>{ "type": "JSON", "json": … }</c> and <c>{ "type": "STRING", "string": … }</c>
    /// forms are understood too.
    /// </summary>
    public JsonElement? Body { get; set; }

    /// <summary>A delay before answering: milliseconds, or MockServer's <c>{ "timeUnit": "SECONDS", "value": 1 }</c>.</summary>
    public JsonElement? Delay { get; set; }
}

/// <summary>
/// How many times an expectation may answer.
/// </summary>
public sealed class MockTimes
{
    /// <summary>The remaining number of answers.</summary>
    public int RemainingTimes { get; set; }

    /// <summary>Ignores <see cref="RemainingTimes"/> when <see langword="true"/>.</summary>
    public bool Unlimited { get; set; }
}
