using Anarchy.Toolkit.Testing.MockServer.Assertion;
using Anarchy.Toolkit.Testing.MockServer.Fixture;
using AwesomeAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Anarchy.Toolkit.Samples.Tests.MockServer;

/// <summary>
/// Uses the server at <c>MOCKSERVER_URL</c> when set (e.g. <c>make test_end_to_end_mocked</c>), otherwise spawns the
/// <c>Samples.MockServer</c> host built next to these tests.
/// </summary>
public sealed class SamplesMockServer : MockServerFixture
{
    protected override void Configure(MockServerProcessOptions options)
    {
        options.AssemblyPath = Path.Combine(AppContext.BaseDirectory, "Anarchy.Toolkit.Samples.MockServer.dll");
        options.Port = 0;
    }
}

/// <summary>
/// Plays the part of an application calling a payments and a stock service, then asserts what reached them.
/// </summary>
public class MockServerTests(SamplesMockServer mock) : IClassFixture<SamplesMockServer>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HttpClient _http = new() { BaseAddress = mock.BaseAddress };

    public async ValueTask InitializeAsync() => await mock.Client.ResetAsync(Ct);

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Payments_AnswerFromTheJsonExpectations()
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");

        var authorized = await _http.PostAsJsonAsync("/payments", new { amount = 10m, currency = "EUR", cardNumber = "4111111111111111" }, Ct);
        var declined = await _http.PostAsJsonAsync("/payments", new { amount = 10m, currency = "EUR", cardNumber = "4000000000000002" }, Ct);
        var captured = await _http.PostAsync("/payments/pay-123/capture", null, Ct);

        authorized.StatusCode.Should().Be(HttpStatusCode.Created);
        authorized.Headers.Location!.ToString().Should().Be("/payments/pay-123");
        declined.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        captured.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payments = await mock.Client.AssertReceivedAsync("POST", "/payments", times: 2);
        payments[0].BodyAs<PaymentRequest>()!.Amount.Should().Be(10m);
        await mock.Client.AssertReceivedAsync("POST", "/payments/{id}/capture", times: 1);
        (await mock.Client.GetRequestsAsync(cancellationToken: Ct)).ShouldAllHaveBeenMatched();
    }

    [Fact]
    public async Task Payments_WithoutABearerToken_AreRejectedByTheMiddleware()
    {
        var response = await _http.PostAsJsonAsync("/payments", new { amount = 10m, currency = "EUR" }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var requests = await mock.Client.AssertReceivedAsync("POST", "/payments", times: 1);
        requests[0].MatchedExpectation.Should().Be("bearer token middleware");
        requests[0].Header("Authorization").Should().BeNull();
    }

    [Fact]
    public async Task Stock_IsComputedByTheMiddlewareUnlessAnExpectationMatches()
    {
        var inStock = await _http.GetFromJsonAsync<StockLevel>("/stock/ABC-1", Ct);
        var soldOut = await _http.GetFromJsonAsync<StockLevel>("/stock/ABC-0", Ct);
        var discontinued = await _http.GetAsync("/stock/DISCONTINUED", Ct);

        inStock!.Available.Should().Be(5);
        soldOut!.Available.Should().Be(0);
        discontinued.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task RuntimeExpectations_OverrideTheFilesForOneScenario()
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");
        await mock.Client.AddExpectationAsync("POST", "/payments", 503, new { error = "maintenance" }, Ct);

        var response = await _http.PostAsJsonAsync("/payments", new { amount = 10m, currency = "EUR" }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        await mock.Client.AssertNotReceivedAsync("POST", "/payments/{id}/capture");
    }

    private sealed record PaymentRequest(decimal Amount, string Currency);

    private sealed record StockLevel(string Sku, int Available);
}
