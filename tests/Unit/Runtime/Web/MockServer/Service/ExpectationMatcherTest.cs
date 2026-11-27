using Anarchy.Toolkit.Runtime.Web.MockServer.Entity;
using Anarchy.Toolkit.Runtime.Web.MockServer.Service;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.MockServer.Service;

public class ExpectationMatcherTest : TestCaseBase
{
    private static MockRequest Request(string method, string path, string? body = null, Dictionary<string, string[]>? headers = null) =>
        new(method, path, new Dictionary<string, string[]> { ["expand"] = ["lines", "customer"] },
            headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["X-Tenant"] = ["nl"] }, body);

    private static MockRequestMatcher Matcher(string json) => JsonSerializer.Deserialize<MockRequestMatcher>(json, MockServerJson.Options)!;

    [Theory]
    [InlineData("/orders/1", "/orders/1", ExpectationMatcher.PathMatch.Exact)]
    [InlineData("/orders/{id}", "/orders/1", ExpectationMatcher.PathMatch.Template)]
    [InlineData("/orders/[0-9]+", "/orders/12", ExpectationMatcher.PathMatch.Pattern)]
    [InlineData("/orders/{id}", "/orders/1/lines", ExpectationMatcher.PathMatch.None)]
    [InlineData(null, "/anything", ExpectationMatcher.PathMatch.Pattern)]
    public void MatchPath_ReturnsHowSpecificallyThePathMatched(string? expected, string actual, ExpectationMatcher.PathMatch result) =>
        ExpectationMatcher.MatchPath(expected, actual).Should().Be(result);

    [Fact]
    public void Match_WhenMethodDiffers_DoesNotMatch() =>
        ExpectationMatcher.Match(Matcher("""{ "method": "POST", "path": "/a" }"""), Request("GET", "/a"))
            .Should().Be(ExpectationMatcher.PathMatch.None);

    [Fact]
    public void Match_WhenQueryAndHeadersArePresent_Matches() =>
        ExpectationMatcher.Match(
                Matcher("""{ "path": "/a", "queryStringParameters": { "expand": ["lines"] }, "headers": { "x-tenant": "n." } }"""),
                Request("GET", "/a"))
            .Should().Be(ExpectationMatcher.PathMatch.Exact);

    [Fact]
    public void Match_WhenAQueryValueIsMissing_DoesNotMatch() =>
        ExpectationMatcher.Match(Matcher("""{ "queryStringParameters": { "expand": ["payments"] } }"""), Request("GET", "/a"))
            .Should().Be(ExpectationMatcher.PathMatch.None);

    [Theory]
    [InlineData("""{ "body": { "order": { "id": 1 } } }""", true)]
    [InlineData("""{ "body": { "order": { "id": 2 } } }""", false)]
    [InlineData("""{ "body": { "type": "JSON", "json": { "order": { "id": 1 } }, "matchType": "STRICT" } }""", false)]
    [InlineData("""{ "body": { "type": "STRING", "string": "\"id\": 1", "subString": true } }""", true)]
    [InlineData("""{ "body": { "type": "REGEX", "regex": ".*lines.*" } }""", true)]
    public void Match_ComparesBodies(string matcher, bool matches)
    {
        const string body = """{ "order": { "id": 1, "lines": [1, 2] } }""";

        var result = ExpectationMatcher.Match(Matcher(matcher), Request("POST", "/a", body));

        (result != ExpectationMatcher.PathMatch.None).Should().Be(matches);
    }
}
