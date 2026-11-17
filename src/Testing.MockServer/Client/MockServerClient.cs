using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Service;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.MockServer.Client;

/// <summary>
/// Talks to a running mock server's <c>/__mock</c> control API: received requests, runtime expectations and resets.
/// </summary>
/// <example>
/// <code language="csharp">
/// var mock = new MockServerClient(new Uri("http://localhost:6000"));
/// await mock.ResetAsync();
/// await mock.AddExpectationAsync("GET", "/stock/{sku}", 200, new { available = 0 });
/// // … exercise the application …
/// await mock.AssertReceivedAsync("GET", "/stock/{sku}", times: 1);
/// </code>
/// </example>
public sealed class MockServerClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    /// <summary>Creates a client for the mock server at <paramref name="baseAddress"/>.</summary>
    /// <param name="baseAddress">The server's base address, e.g. <c>http://localhost:6000</c>.</param>
    public MockServerClient(Uri baseAddress) : this(new HttpClient { BaseAddress = baseAddress }) => _ownsHttp = true;

    /// <summary>Creates a client over an existing <see cref="HttpClient"/> whose base address is the server's.</summary>
    /// <param name="http">The HTTP client.</param>
    public MockServerClient(HttpClient http) => _http = http;

    /// <summary>The server's base address.</summary>
    public Uri BaseAddress => _http.BaseAddress!;

    /// <summary>Whether the server answers its health check.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns><see langword="true"/> when healthy.</returns>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync("/__mock/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <summary>Waits until the server is healthy.</summary>
    /// <param name="timeout">How long to wait.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <exception cref="TimeoutException">It did not become healthy in time.</exception>
    public async Task WaitUntilHealthyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!await IsHealthyAsync(cancellationToken))
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException($"The mock server at {BaseAddress} did not become healthy within {timeout}.");
            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>The recorded requests, oldest first, optionally filtered.</summary>
    /// <param name="method">Only this HTTP method.</param>
    /// <param name="path">Only paths matching this exact path, template or regular expression.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The requests.</returns>
    public async Task<IReadOnlyList<RecordedRequest>> GetRequestsAsync(
        string? method = null, string? path = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (method is not null) query.Add($"method={Uri.EscapeDataString(method)}");
        if (path is not null) query.Add($"path={Uri.EscapeDataString(path)}");
        var url = "/__mock/requests" + (query.Count > 0 ? "?" + string.Join('&', query) : "");
        return await _http.GetFromJsonAsync<List<RecordedRequest>>(url, MockServerJson.Options, cancellationToken) ?? [];
    }

    /// <summary>Forgets the recorded requests.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task ClearRequestsAsync(CancellationToken cancellationToken = default) =>
        (await _http.DeleteAsync("/__mock/requests", cancellationToken)).EnsureSuccessStatusCode();

    /// <summary>Adds runtime expectations, which take precedence over file ones of the same priority.</summary>
    /// <param name="expectations">The expectations.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task AddExpectationAsync(IEnumerable<MockExpectation> expectations, CancellationToken cancellationToken = default) =>
        AddExpectationJsonAsync(JsonSerializer.Serialize(expectations, MockServerJson.Options), cancellationToken);

    /// <summary>Adds a runtime expectation.</summary>
    /// <param name="expectation">The expectation.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task AddExpectationAsync(MockExpectation expectation, CancellationToken cancellationToken = default) =>
        AddExpectationAsync([expectation], cancellationToken);

    /// <summary>Adds a simple runtime expectation.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path (exact, templated or regular expression).</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="body">The body: a string as-is, anything else as JSON.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public Task AddExpectationAsync(string method, string path, int statusCode = 200, object? body = null, CancellationToken cancellationToken = default) =>
        AddExpectationAsync(new MockExpectation {
            HttpRequest = new MockRequestMatcher { Method = method, Path = path },
            HttpResponse = new MockResponse {
                StatusCode = statusCode,
                Body = body is null ? null : JsonSerializer.SerializeToElement(body, MockServerJson.Options),
            },
        }, cancellationToken);

    /// <summary>Adds runtime expectations from JSON (one object or an array, as in expectation files).</summary>
    /// <param name="json">The JSON.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task AddExpectationJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsync("/__mock/expectations", new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"The mock server rejected the expectations: {await response.Content.ReadAsStringAsync(cancellationToken)}");
    }

    /// <summary>Removes runtime expectations, or only the one with <paramref name="id"/>.</summary>
    /// <param name="id">The expectation id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task RemoveExpectationsAsync(string? id = null, CancellationToken cancellationToken = default) =>
        (await _http.DeleteAsync("/__mock/expectations" + (id is null ? "" : $"?id={Uri.EscapeDataString(id)}"), cancellationToken)).EnsureSuccessStatusCode();

    /// <summary>Forgets recorded requests and runtime expectations.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task ResetAsync(CancellationToken cancellationToken = default) =>
        (await _http.PostAsync("/__mock/reset", null, cancellationToken)).EnsureSuccessStatusCode();

    /// <summary>Reloads the expectation files.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task ReloadAsync(CancellationToken cancellationToken = default) =>
        (await _http.PostAsync("/__mock/reload", null, cancellationToken)).EnsureSuccessStatusCode();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}
