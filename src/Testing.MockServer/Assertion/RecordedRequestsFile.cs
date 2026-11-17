using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.MockServer.Assertion;

/// <summary>
/// Reads the JSON-lines file a mock server writes with <c>--record</c>, e.g. to assert after the server stopped.
/// </summary>
public static class RecordedRequestsFile
{
    /// <summary>Reads every recorded request, oldest first; an absent file reads as none.</summary>
    /// <param name="path">The record file.</param>
    /// <returns>The requests.</returns>
    public static IReadOnlyList<RecordedRequest> Read(string path)
    {
        if (!File.Exists(path)) return [];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var requests = new List<RecordedRequest>();
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            requests.Add(JsonSerializer.Deserialize<RecordedRequest>(line, MockServerJson.Options)!);
        }
        return requests;
    }
}
