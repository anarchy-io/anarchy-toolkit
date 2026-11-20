using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Configuration.Telemetry;

/// <summary>
/// Async-local holder for AWS request values (currently the X-Ray trace id) of the request being processed.
/// </summary>
/// <remarks>
/// Set it at the start of each request/invocation (e.g. in a Lambda handler) with <see cref="SetValues"/>; values flow
/// with the current async context. Read by <see cref="ApplicationContext"/>.
/// </remarks>
public static class AmazonRequestContext
{
    private static readonly AsyncLocal<AmazonRequestValues> Values = new();

    /// <summary>
    /// Gets the trace id for the current async flow, or <c>"Unknown"</c> when not set.
    /// </summary>
    public static string AmazonTraceId {
        get => Values.Value?.AmazonTraceId ?? "Unknown";
    }

    /// <summary>
    /// Sets the request values for the current async flow.
    /// </summary>
    /// <param name="amazonTraceId">The trace id (e.g. from the <c>X-Amzn-Trace-Id</c> header); <see langword="null"/> stores <c>"Unknown"</c>.</param>
    public static void SetValues(string? amazonTraceId)
    {
        Values.Value = new AmazonRequestValues(amazonTraceId);
    }

    private class AmazonRequestValues(string? amazonTraceId)
    {
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
        public string AmazonTraceId { get; } = amazonTraceId ?? "Unknown";
    }
}
