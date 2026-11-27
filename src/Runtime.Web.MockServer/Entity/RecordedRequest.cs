namespace Anarchy.Toolkit.Runtime.Web.MockServer.Entity;

/// <summary>
/// A request the mock server received, with how it was answered. Written as one JSON line to the record file and
/// returned by <c>GET /__mock/requests</c>.
/// </summary>
public sealed class RecordedRequest
{
    /// <summary>A sequence number, starting at 1 for each server run.</summary>
    public long Sequence { get; set; }

    /// <summary>When it was received.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>The HTTP method.</summary>
    public string Method { get; set; } = "";

    /// <summary>The path, without the query string.</summary>
    public string Path { get; set; } = "";

    /// <summary>The query string, including the leading <c>?</c>, or empty.</summary>
    public string QueryString { get; set; } = "";

    /// <summary>The query parameters.</summary>
    public Dictionary<string, string[]> Query { get; set; } = [];

    /// <summary>The request headers.</summary>
    public Dictionary<string, string[]> Headers { get; set; } = [];

    /// <summary>The request body as text, or <see langword="null"/> when empty.</summary>
    public string? Body { get; set; }

    /// <summary>The expectation that answered, or <see langword="null"/> when none matched (or a middleware answered).</summary>
    public string? MatchedExpectation { get; set; }

    /// <summary>The response status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>How long it took to answer, in milliseconds.</summary>
    public double DurationMs { get; set; }
}
