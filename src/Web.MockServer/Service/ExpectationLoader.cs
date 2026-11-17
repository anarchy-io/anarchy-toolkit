using Anarchy.Toolkit.Web.MockServer.Entity;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.MockServer.Service;

/// <summary>
/// Loads <see cref="MockExpectation"/>s from JSON files, each holding one expectation or an array of them.
/// </summary>
public static class ExpectationLoader
{
    /// <summary>The outcome of loading a set of sources.</summary>
    /// <param name="Expectations">Every expectation loaded, in file order.</param>
    /// <param name="Files">Each file read and how many expectations it held.</param>
    /// <param name="Errors">Files that could not be read or parsed, with the reason.</param>
    public sealed record Result(
        IReadOnlyList<MockExpectation> Expectations,
        IReadOnlyList<(string File, int Count)> Files,
        IReadOnlyList<(string Source, string Error)> Errors);

    /// <summary>
    /// Expands <paramref name="sources"/> into files: a file is used as-is, a directory contributes every <c>*.json</c>
    /// below it, and anything else is a glob pattern (e.g. <c>mocks/**/*.json</c>) relative to the current directory.
    /// </summary>
    /// <param name="sources">The sources.</param>
    /// <returns>The files, sorted and without duplicates, and the sources that matched nothing.</returns>
    public static (IReadOnlyList<string> Files, IReadOnlyList<string> Missing) Resolve(IEnumerable<string> sources)
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);
        var missing = new List<string>();

        foreach (var source in sources)
        {
            var before = files.Count;
            var full = Path.GetFullPath(source);
            if (File.Exists(full)) files.Add(full);
            else if (Directory.Exists(full)) files.UnionWith(Directory.EnumerateFiles(full, "*.json", SearchOption.AllDirectories));
            else
            {
                var (root, pattern) = SplitGlob(source);
                if (Directory.Exists(root))
                {
                    var matcher = new Matcher(StringComparison.OrdinalIgnoreCase).AddInclude(pattern);
                    files.UnionWith(matcher.GetResultsInFullPath(root));
                }
            }
            if (files.Count == before) missing.Add(source);
        }

        return (files.ToList(), missing);
    }

    /// <summary>
    /// Loads every expectation from <paramref name="sources"/> (see <see cref="Resolve"/>).
    /// </summary>
    /// <param name="sources">Files, directories or glob patterns.</param>
    /// <returns>The expectations, the files read and the errors.</returns>
    public static Result Load(IEnumerable<string> sources)
    {
        var (files, missing) = Resolve(sources);
        var expectations = new List<MockExpectation>();
        var loaded = new List<(string, int)>();
        var errors = missing.Select(source => (source, "matched no file")).ToList();

        foreach (var file in files)
        {
            try
            {
                var fromFile = Parse(File.ReadAllText(file), MockServerJson.DisplayPath(file));
                expectations.AddRange(fromFile);
                loaded.Add((file, fromFile.Count));
            }
            catch (Exception exception) when (exception is JsonException or IOException or InvalidDataException)
            {
                errors.Add((file, exception.Message));
            }
        }

        return new Result(expectations, loaded, errors);
    }

    /// <summary>
    /// Parses one expectation or an array of them.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <param name="source">Recorded as each expectation's <see cref="MockExpectation.Source"/>, suffixed with its index.</param>
    /// <returns>The expectations.</returns>
    /// <exception cref="JsonException">The JSON is invalid.</exception>
    /// <exception cref="InvalidDataException">The JSON is neither an object nor an array of objects.</exception>
    public static IReadOnlyList<MockExpectation> Parse(string json, string source = "runtime")
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var root = document.RootElement;
        var elements = root.ValueKind switch {
            JsonValueKind.Array => root.EnumerateArray().ToList(),
            JsonValueKind.Object => [root],
            _ => throw new InvalidDataException("Expected an expectation object or an array of them."),
        };

        return elements.Select((element, index) => {
            var expectation = element.Deserialize<MockExpectation>(MockServerJson.Options)
                              ?? throw new InvalidDataException($"Expectation #{index} is null.");
            expectation.Source = elements.Count == 1 ? source : $"{source}#{index}";
            return expectation;
        }).ToList();
    }

    private static (string Root, string Pattern) SplitGlob(string glob)
    {
        var parts = glob.Replace('\\', '/').Split('/');
        var firstWildcard = Array.FindIndex(parts, p => p.IndexOfAny(['*', '?', '[', '{']) >= 0);
        if (firstWildcard <= 0)
            return firstWildcard == 0 ? (Directory.GetCurrentDirectory(), glob) : (Path.GetDirectoryName(Path.GetFullPath(glob)) ?? ".", Path.GetFileName(glob));

        var root = string.Join('/', parts[..firstWildcard]);
        return (Path.GetFullPath(root.Length == 0 ? "/" : root), string.Join('/', parts[firstWildcard..]));
    }
}
