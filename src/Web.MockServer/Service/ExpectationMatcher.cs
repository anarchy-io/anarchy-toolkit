using Anarchy.Toolkit.Web.MockServer.Entity;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Web.MockServer.Service;

/// <summary>
/// The request as seen by the matcher: method, path, query, headers and body text.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path, without the query string.</param>
/// <param name="Query">The query parameters.</param>
/// <param name="Headers">The headers (case-insensitive names).</param>
/// <param name="Body">The body text, or <see langword="null"/>.</param>
public sealed record MockRequest(
    string Method,
    string Path,
    IReadOnlyDictionary<string, string[]> Query,
    IReadOnlyDictionary<string, string[]> Headers,
    string? Body)
{
    /// <summary>Builds one from an ASP.NET Core request and its already-read body.</summary>
    /// <param name="request">The request.</param>
    /// <param name="body">The body text.</param>
    /// <returns>The matcher view of the request.</returns>
    public static MockRequest From(HttpRequest request, string? body) => new(
        request.Method,
        request.Path.Value ?? "/",
        request.Query.ToDictionary(q => q.Key, q => q.Value.Select(v => v ?? "").ToArray(), StringComparer.Ordinal),
        request.Headers.ToDictionary(h => h.Key, h => h.Value.Select(v => v ?? "").ToArray(), StringComparer.OrdinalIgnoreCase),
        body);
}

/// <summary>
/// Decides whether a request matches a <see cref="MockRequestMatcher"/>, and how specifically.
/// </summary>
public static class ExpectationMatcher
{
    /// <summary>How specifically a path matched: exact paths beat templates, which beat regular expressions.</summary>
    public enum PathMatch
    {
        /// <summary>It did not match.</summary>
        None,

        /// <summary>It matched as a regular expression, or no path was required.</summary>
        Pattern,

        /// <summary>It matched a <c>{param}</c> template.</summary>
        Template,

        /// <summary>It matched exactly.</summary>
        Exact,
    }

    /// <summary>
    /// Matches <paramref name="request"/> against <paramref name="matcher"/>.
    /// </summary>
    /// <param name="matcher">The expectation's request side.</param>
    /// <param name="request">The request.</param>
    /// <returns>How the path matched, or <see cref="PathMatch.None"/> when anything does not match.</returns>
    public static PathMatch Match(MockRequestMatcher matcher, MockRequest request)
    {
        if (matcher.Method is { Length: > 0 } method && !string.Equals(method, request.Method, StringComparison.OrdinalIgnoreCase))
            return PathMatch.None;

        var path = MatchPath(matcher.Path, request.Path);
        if (path == PathMatch.None) return PathMatch.None;
        if (!MatchValues(matcher.QueryStringParameters, request.Query, StringComparer.Ordinal)) return PathMatch.None;
        if (!MatchValues(matcher.Headers, request.Headers, StringComparer.OrdinalIgnoreCase)) return PathMatch.None;
        if (matcher.Body is { } body && !MatchBody(body, request.Body)) return PathMatch.None;
        return path;
    }

    /// <summary>How specifically <paramref name="actual"/> matches <paramref name="expected"/> (see <see cref="MockRequestMatcher.Path"/>).</summary>
    /// <param name="expected">The exact path, template or regular expression; any path when empty.</param>
    /// <param name="actual">The request path.</param>
    /// <returns>How it matched.</returns>
    public static PathMatch MatchPath(string? expected, string actual)
    {
        if (string.IsNullOrEmpty(expected)) return PathMatch.Pattern;
        if (string.Equals(expected.TrimEnd('/'), actual.TrimEnd('/'), StringComparison.Ordinal)) return PathMatch.Exact;

        if (expected.Contains('{') && !expected.Contains('\\'))
        {
            var expectedSegments = expected.Trim('/').Split('/');
            var actualSegments = actual.Trim('/').Split('/');
            if (expectedSegments.Length == actualSegments.Length && expectedSegments.Zip(actualSegments).All(pair =>
                    pair.First.StartsWith('{') && pair.First.EndsWith('}') && pair.Second.Length > 0 || pair.First == pair.Second))
                return PathMatch.Template;
        }

        return IsFullMatch(expected, actual) ? PathMatch.Pattern : PathMatch.None;
    }

    private static bool MatchValues(
        Dictionary<string, JsonElement>? expected, IReadOnlyDictionary<string, string[]> actual, StringComparer comparer)
    {
        if (expected is null) return true;
        var lookup = actual.ToDictionary(pair => pair.Key, pair => pair.Value, comparer);
        foreach (var (name, value) in expected)
        {
            if (!lookup.TryGetValue(name, out var values)) return false;
            foreach (var wanted in ValuesOf(value))
            {
                if (!values.Any(v => v == wanted || IsFullMatch(wanted, v))) return false;
            }
        }
        return true;
    }

    /// <summary>The string values of a header or query parameter given as a value or a list of values.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns>The values.</returns>
    public static IEnumerable<string> ValuesOf(JsonElement value) => value.ValueKind switch {
        JsonValueKind.Array => value.EnumerateArray().Select(AsText),
        JsonValueKind.Null or JsonValueKind.Undefined => [],
        _ => [AsText(value)],
    };

    private static string AsText(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText();

    private static bool MatchBody(JsonElement expected, string? actual)
    {
        actual ??= "";
        if (expected.ValueKind == JsonValueKind.String) return expected.GetString() == actual;

        if (expected.ValueKind == JsonValueKind.Object && expected.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
        {
            switch (type.GetString()?.ToUpperInvariant())
            {
                case "JSON" when expected.TryGetProperty("json", out var json):
                    var strict = expected.TryGetProperty("matchType", out var mt) && mt.GetString()?.ToUpperInvariant() == "STRICT";
                    return MatchJsonText(json.ValueKind == JsonValueKind.String ? Parse(json.GetString()) : json, actual, strict);
                case "STRING" when expected.TryGetProperty("string", out var text):
                    var sub = expected.TryGetProperty("subString", out var s) && s.ValueKind == JsonValueKind.True;
                    return sub ? actual.Contains(text.GetString() ?? "", StringComparison.Ordinal) : actual == text.GetString();
                case "REGEX" when expected.TryGetProperty("regex", out var regex):
                    return IsFullMatch(regex.GetString() ?? "", actual);
            }
        }

        return MatchJsonText(expected, actual, strict: false);
    }

    private static bool MatchJsonText(JsonElement? expected, string actual, bool strict)
    {
        if (expected is null) return false;
        var parsed = Parse(actual);
        return parsed is { } doc && JsonContains(expected.Value, doc, strict);
    }

    private static JsonElement? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Whether <paramref name="actual"/> contains <paramref name="expected"/> (every expected property and array item).</summary>
    /// <param name="expected">The expected JSON.</param>
    /// <param name="actual">The actual JSON.</param>
    /// <param name="strict">Requires equal objects and arrays instead of containment.</param>
    /// <returns><see langword="true"/> when it matches.</returns>
    public static bool JsonContains(JsonElement expected, JsonElement actual, bool strict = false)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                if (actual.ValueKind != JsonValueKind.Object) return false;
                if (strict && expected.EnumerateObject().Count() != actual.EnumerateObject().Count()) return false;
                return expected.EnumerateObject().All(property =>
                    actual.TryGetProperty(property.Name, out var value) && JsonContains(property.Value, value, strict));
            case JsonValueKind.Array:
                if (actual.ValueKind != JsonValueKind.Array) return false;
                var actualItems = actual.EnumerateArray().ToList();
                if (strict)
                {
                    return actualItems.Count == expected.GetArrayLength()
                           && expected.EnumerateArray().Zip(actualItems).All(pair => JsonContains(pair.First, pair.Second, true));
                }
                return expected.EnumerateArray().All(item => actualItems.Any(candidate => JsonContains(item, candidate)));
            case JsonValueKind.Number:
                return actual.ValueKind == JsonValueKind.Number && actual.GetDecimal() == expected.GetDecimal();
            default:
                return expected.ValueKind == actual.ValueKind && expected.GetRawText() == actual.GetRawText();
        }
    }

    private static bool IsFullMatch(string pattern, string input)
    {
        try
        {
            return Regex.IsMatch(input, $"^(?:{pattern})$", RegexOptions.None, TimeSpan.FromMilliseconds(250));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
