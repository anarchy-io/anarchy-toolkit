using Anarchy.Toolkit.Testing.MockServer.Client;
using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;
using AwesomeAssertions.Execution;
using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.MockServer.Assertion;

/// <summary>
/// Assertions on the requests a mock server received, from <see cref="MockServerClient"/> or a record file
/// (<see cref="RecordedRequestsFile"/>). Failures list the requests that were received.
/// </summary>
public static class RecordedRequestAssertions
{
    /// <summary>
    /// Asserts requests matching <paramref name="method"/>, <paramref name="path"/> and <paramref name="predicate"/>
    /// were received: <paramref name="times"/> times exactly, or at least once.
    /// </summary>
    /// <param name="requests">The received requests.</param>
    /// <param name="method">The HTTP method, case-insensitive.</param>
    /// <param name="path">An exact path, a <c>{param}</c> template or a regular expression.</param>
    /// <param name="times">The exact number expected; at least one when <see langword="null"/>.</param>
    /// <param name="predicate">An extra condition, e.g. on the body.</param>
    /// <returns>The matching requests.</returns>
    public static IReadOnlyList<RecordedRequest> ShouldHaveReceived(
        this IEnumerable<RecordedRequest> requests, string method, string path, int? times = null, Func<RecordedRequest, bool>? predicate = null)
    {
        var all = requests.ToList();
        var matching = Filter(all, method, path, predicate);
        if (times is { } expected ? matching.Count != expected : matching.Count == 0)
            Fail($"Expected the mock server to have received {method} {path} {(times is null ? "at least once" : $"{times} time(s)")}"
                        + $"{(predicate is null ? "" : " matching the predicate")}, but it was received {matching.Count} time(s).{Describe(all)}");
        return matching;
    }

    /// <summary>Asserts no request matching <paramref name="method"/>, <paramref name="path"/> and <paramref name="predicate"/> was received.</summary>
    /// <param name="requests">The received requests.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">An exact path, template or regular expression.</param>
    /// <param name="predicate">An extra condition.</param>
    public static void ShouldNotHaveReceived(
        this IEnumerable<RecordedRequest> requests, string method, string path, Func<RecordedRequest, bool>? predicate = null)
    {
        var all = requests.ToList();
        var matching = Filter(all, method, path, predicate);
        if (matching.Count > 0) Fail($"Expected the mock server not to have received {method} {path}, but it was received {matching.Count} time(s).{Describe(all)}");
    }

    /// <summary>Asserts no received request went unanswered by an expectation or middleware.</summary>
    /// <param name="requests">The received requests.</param>
    public static void ShouldAllHaveBeenMatched(this IEnumerable<RecordedRequest> requests)
    {
        var unmatched = requests.Where(r => r.MatchedExpectation is null).ToList();
        if (unmatched.Count > 0) Fail($"Expected every request to match an expectation, but {unmatched.Count} did not.{Describe(unmatched)}");
    }

    /// <summary>Fetches the received requests and asserts as <see cref="ShouldHaveReceived"/>.</summary>
    /// <param name="client">The mock server client.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">An exact path, template or regular expression.</param>
    /// <param name="times">The exact number expected; at least one when <see langword="null"/>.</param>
    /// <param name="predicate">An extra condition.</param>
    /// <returns>The matching requests.</returns>
    public static async Task<IReadOnlyList<RecordedRequest>> AssertReceivedAsync(
        this MockServerClient client, string method, string path, int? times = null, Func<RecordedRequest, bool>? predicate = null) =>
        (await client.GetRequestsAsync()).ShouldHaveReceived(method, path, times, predicate);

    /// <summary>Fetches the received requests and asserts as <see cref="ShouldNotHaveReceived"/>.</summary>
    /// <param name="client">The mock server client.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">An exact path, template or regular expression.</param>
    /// <param name="predicate">An extra condition.</param>
    public static async Task AssertNotReceivedAsync(
        this MockServerClient client, string method, string path, Func<RecordedRequest, bool>? predicate = null) =>
        (await client.GetRequestsAsync()).ShouldNotHaveReceived(method, path, predicate);

    /// <summary>Deserializes the request body.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="request">The request.</param>
    /// <returns>The body, or <see langword="default"/> when empty.</returns>
    public static T? BodyAs<T>(this RecordedRequest request) =>
        string.IsNullOrEmpty(request.Body) ? default : JsonSerializer.Deserialize<T>(request.Body, MockServerJson.Options);

    /// <summary>A header's first value, case-insensitive.</summary>
    /// <param name="request">The request.</param>
    /// <param name="name">The header name.</param>
    /// <returns>The value, or <see langword="null"/>.</returns>
    public static string? Header(this RecordedRequest request, string name) =>
        request.Headers.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

    private static List<RecordedRequest> Filter(List<RecordedRequest> requests, string method, string path, Func<RecordedRequest, bool>? predicate) =>
        requests.Where(r => string.Equals(r.Method, method, StringComparison.OrdinalIgnoreCase)
                            && ExpectationMatcher.MatchPath(path, r.Path) != ExpectationMatcher.PathMatch.None
                            && (predicate is null || predicate(r))).ToList();

    private static void Fail(string message) =>
        AssertionChain.GetOrCreate().FailWith(message.Replace("{", "{{").Replace("}", "}}"));

    private static string Describe(IReadOnlyCollection<RecordedRequest> requests)
    {
        if (requests.Count == 0) return " No requests were received.";
        var text = new StringBuilder("\nReceived:");
        foreach (var r in requests.TakeLast(20))
            text.Append($"\n  #{r.Sequence} {r.Method} {r.Path}{r.QueryString} → {r.StatusCode} ({r.MatchedExpectation ?? "unmatched"})");
        if (requests.Count > 20) text.Append($"\n  … and {requests.Count - 20} earlier");
        return text.ToString();
    }
}
