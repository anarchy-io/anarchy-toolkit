namespace Anarchy.Toolkit.Runtime.Web.MockServer.Entity;

/// <summary>The kind of a <see cref="MockServerLogEntry"/>.</summary>
public enum MockServerLogLevel
{
    /// <summary>Diagnostic detail, e.g. from ASP.NET Core.</summary>
    Debug,

    /// <summary>A handled request.</summary>
    Request,

    /// <summary>An informational message.</summary>
    Information,

    /// <summary>A warning.</summary>
    Warning,

    /// <summary>An error.</summary>
    Error,
}

/// <summary>
/// One entry of the mock server's log, kept in memory whatever the console verbosity and returned by
/// <c>GET /__mock/logs</c>.
/// </summary>
public sealed class MockServerLogEntry
{
    /// <summary>A sequence number, starting at 1 for each server run.</summary>
    public long Sequence { get; set; }

    /// <summary>When it was written.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>The kind of entry.</summary>
    public MockServerLogLevel Level { get; set; }

    /// <summary>Where it comes from: <c>mockserver</c>, or a logger category.</summary>
    public string Category { get; set; } = "mockserver";

    /// <summary>The message, as plain text.</summary>
    public string Message { get; set; } = "";

    /// <summary>The request, for <see cref="MockServerLogLevel.Request"/> entries.</summary>
    public RecordedRequest? Request { get; set; }
}
