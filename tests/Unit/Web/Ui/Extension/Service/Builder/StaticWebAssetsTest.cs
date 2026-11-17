using Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Extension.Service.Builder;

public class StaticWebAssetsTest
{
    private static readonly string Manifest = Path.Combine(AppContext.BaseDirectory, "Anarchy.Toolkit.Web.Ui.staticwebassets.runtime.json");

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task UseToolkitUiStaticWebAssets_ServesAssetsOfUnpublishedBuilds(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer().UseSetting(WebHostDefaults.StaticWebAssetsKey, Manifest);

        await using var app = builder.Build();
        app.UseToolkitUiStaticWebAssets().UseToolkitUiStaticWebAssets();
        app.MapStaticAssets(Manifest.Replace(".runtime.json", ".endpoints.json"));
        await app.StartAsync(TestContext.Current.CancellationToken);

        var stylesheet = await app.GetTestClient().GetByteArrayAsync("/css/toolkit-ui.css", TestContext.Current.CancellationToken);

        stylesheet.Should().NotBeEmpty();
    }
}
