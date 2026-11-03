using Anarchy.Toolkit.Samples.Ui.Composition;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Anarchy.Toolkit.Samples.Tests.Ui;

/// <summary>
/// Smoke tests of the sample UI: the page shell with its assets, and the stub auth service and orders API it
/// logs in and calls through.
/// </summary>
public class SamplesUiTests(WebApplicationFactory<Startup> factory) : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    [Fact]
    public async Task LoginPage_RendersTheShellWithToolkitAndAppStylesheets()
    {
        var html = await _client.GetStringAsync("ui/login", TestContext.Current.CancellationToken);

        html.Should().Contain("toolkit-ui.css").And.Contain("samples-ui.css").And.Contain("_framework/blazor.web.js");
    }

    [Theory]
    [InlineData("toolkit-ui.css")]
    [InlineData("toolkit-ui.js")]
    [InlineData("favicon.png")]
    public async Task ToolkitAssets_AreServed(string path)
    {
        using var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StubToken_WithWrongSecret_IsRejected()
    {
        using var response = await RequestTokenAsync("samples-ui", "wrong");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StubOrders_WithoutToken_IsRejected()
    {
        using var response = await _client.GetAsync("stub/development/orders", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StubOrders_WithIssuedToken_ReturnsTheEnvironmentsOrders()
    {
        var ct = TestContext.Current.CancellationToken;
        using var tokenResponse = await RequestTokenAsync("samples-ui", "samples-ui-secret");
        var token = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, "stub/testing/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("access_token").GetString());
        using var response = await _client.SendAsync(request, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        orders.GetArrayLength().Should().Be(23);
        orders[0].GetProperty("id").GetString().Should().StartWith("TST-");
    }

    private Task<HttpResponseMessage> RequestTokenAsync(string clientId, string clientSecret) =>
        _client.PostAsync("stub/auth/connect/token", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        }), TestContext.Current.CancellationToken);
}
