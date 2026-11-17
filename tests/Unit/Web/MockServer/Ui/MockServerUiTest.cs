using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Web.MockServer;
using Anarchy.Toolkit.Web.MockServer.Entity;
using Anarchy.Toolkit.Web.MockServer.Ui.Extension;
using AwesomeAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Anarchy.Toolkit.Tests.Unit.Web.MockServer.Ui;

public class MockServerUiTest : TestCaseBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<MockServerHost> StartAsync(bool ui) =>
        MockServerHost.StartAsync(new MockServerOptions { Port = 0, Verbosity = MockServerVerbosity.Quiet, Ui = ui }, server => server.UseUi());

    [Theory]
    [InlineData("")]
    [InlineData("requests")]
    [InlineData("middlewares")]
    [InlineData("logs")]
    public async Task UseUi_ServesPagesWithoutRecordingThem(string page)
    {
        await using var host = await StartAsync(ui: true);
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync($"/__mock/ui/{page}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).Should().Contain("""<base href="/__mock/ui/" />""");
        (await client.GetFromJsonAsync<List<RecordedRequest>>("/__mock/requests", Ct)).Should().BeEmpty();
    }

    [Fact]
    public async Task UseUi_ServesTheBlazorScript()
    {
        await using var host = await StartAsync(ui: true);
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var script = await client.GetStringAsync("/__mock/ui/_framework/blazor.web.js", Ct);

        script.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UseUi_RedirectsToTheTrailingSlash()
    {
        await using var host = await StartAsync(ui: true);
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync("/__mock/ui", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/__mock/ui/");
    }

    [Fact]
    public async Task UseUi_WithNoUi_ServesNothing()
    {
        await using var host = await StartAsync(ui: false);
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        (await client.GetAsync("/__mock/ui/", Ct)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
