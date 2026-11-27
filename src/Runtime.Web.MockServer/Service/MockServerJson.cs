using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Runtime.Web.MockServer.Service;

/// <summary>
/// The JSON settings used for expectations, recordings and the control API: camelCase, case-insensitive on read.
/// </summary>
public static class MockServerJson
{
    /// <summary>The shared serializer options.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Compact options for one-line records.</summary>
    /// <summary>A path relative to the current directory when below it, otherwise its file name.</summary>
    internal static string DisplayPath(string path)
    {
        var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
        return relative.StartsWith("..") ? Path.GetFileName(path) : relative;
    }

    internal static JsonSerializerOptions Compact { get; } = new(Options) { WriteIndented = false };
}
