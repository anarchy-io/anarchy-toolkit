using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Configuration.Entity.Observability;

public static class AmazonRequestContext
{
    private static readonly AsyncLocal<AmazonRequestValues> Values = new();

    public static string AmazonTraceId {
        get => Values.Value?.AmazonTraceId ?? "Unknown";
    }

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
