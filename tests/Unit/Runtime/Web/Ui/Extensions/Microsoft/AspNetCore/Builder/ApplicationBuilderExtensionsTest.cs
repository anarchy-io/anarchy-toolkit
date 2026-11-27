using Anarchy.Toolkit.Runtime.Web.Ui.Extensions.Microsoft.AspNetCore.Builder;
using Anarchy.Toolkit.Runtime.Web.Ui.Extensions.Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Ui.Extensions.Microsoft.AspNetCore.Builder;

public class ApplicationBuilderExtensionsTest
{
    [Fact]
    public async Task UseToolkitUi_InsideMapBranch_ServesEmbeddedAssetsUnderSubPath()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddToolkitUi();

        await using var app = builder.Build();
        app.Map("/__mock/ui", branch => branch.UseToolkitUi());
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestClient();
        var ui = await client.GetAsync("/__mock/ui/css/toolkit-ui.css", TestContext.Current.CancellationToken);
        var root = await client.GetAsync("/css/toolkit-ui.css", TestContext.Current.CancellationToken);

        ui.StatusCode.Should().Be(HttpStatusCode.OK);
        root.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
