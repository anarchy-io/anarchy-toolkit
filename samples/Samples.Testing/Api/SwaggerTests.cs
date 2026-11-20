using Anarchy.Toolkit.Samples.Api.Composition;
using Anarchy.Toolkit.Web.Entity.Swagger;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Anarchy.Toolkit.Samples.Testing.Api;

/// <summary>
/// Checks the Swagger document and the default Swagger UI theme served by the example API.
/// </summary>
public class SwaggerTests(WebApplicationFactory<Startup> factory) : IClassFixture<WebApplicationFactory<Startup>>
{
    [Fact]
    public async Task SwaggerUi_LinksTheThemeStylesheetsAndFavicon()
    {
        var html = await factory.CreateClient().GetStringAsync("swagger/index.html", TestContext.Current.CancellationToken);

        html.Should().Contain("css/swagger-colors.css")
            .And.Contain("css/swagger-theme.css")
            .And.Contain("js/swagger-theme.js")
            .And.Contain("images/logo.png")
            .And.Contain("images/logo-dark.png")
            .And.Contain("favicon.png")
            .And.Contain("favicon.svg");
    }

    [Theory]
    [InlineData("css/swagger-colors.css", "--swagger-theme-primary")]
    [InlineData("css/swagger-theme.css", ".swagger-ui")]
    [InlineData("js/swagger-theme.js", "dark-mode")]
    public async Task ThemeAssets_AreServed(string path, string expected)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(expected);
    }

    [Theory]
    [InlineData("favicon.png")]
    [InlineData("favicon.svg")]
    [InlineData("images/logo.png")]
    [InlineData("images/logo-dark.png")]
    public async Task Images_AreServed(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerUi_KeepsItsOwnLookWhenTheThemeIsDisabled()
    {
        var client = factory
            .WithWebHostBuilder(host => host.ConfigureServices(services =>
                services.Configure<SwaggerThemeOptions>(theme => theme.Enabled = false)))
            .CreateClient();

        var html = await client.GetStringAsync("swagger/index.html", TestContext.Current.CancellationToken);

        html.Should().NotContain("swagger-theme").And.NotContain("images/logo");
    }

    [Fact]
    public async Task ThemeColors_CanBeReplacedFromTheApplicationWebRoot()
    {
        var webRoot = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(webRoot, "css"));
            await File.WriteAllTextAsync(Path.Combine(webRoot, "css", "swagger-colors.css"), ":root { --swagger-theme-primary: #0000FF; }",
                TestContext.Current.CancellationToken);
            var client = factory.WithWebHostBuilder(host => host.UseWebRoot(webRoot)).CreateClient();

            var colors = await client.GetStringAsync("css/swagger-colors.css", TestContext.Current.CancellationToken);
            var theme = await client.GetAsync("css/swagger-theme.css", TestContext.Current.CancellationToken);

            colors.Should().Contain("#0000FF");
            theme.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SwaggerDocument_IsOpenApi32()
    {
        var json = await factory.CreateClient().GetStringAsync("swagger/spec/swagger.json", TestContext.Current.CancellationToken);

        json.Should().Contain("\"openapi\": \"3.2");
    }
}
