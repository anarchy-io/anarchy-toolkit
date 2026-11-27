using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Runtime.Web.Swagger.Extensions.Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Swagger.Extensions.Microsoft.Extensions.DependencyInjection;

public class SwaggerServiceCollectionExtensionsTest
{
    private sealed class AuthSettings : IAuthServiceAppSettings
    {
        public AuthServiceSettings AuthService { get; init; } = new() {
            Host = new Uri("https://auth.example.local/"),
            ClientId = "orders",
            ClientSecretName = "orders-secret"
        };
    }

    private sealed class PlainSettings : IAppSettings;

    private static async Task<WebApplication> StartAsync(IAppSettings settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers();
        builder.Services.AddToolkitSwagger("Orders API", settings, builder.Environment, ["orders:read"], titlePrefix: "[Orders]");

        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    [Fact]
    public async Task AddToolkitSwagger_ServesTheDocumentWithTheOAuth2SchemeAndRedirectsTheRoot()
    {
        await using var app = await StartAsync(new AuthSettings());
        var client = app.GetTestClient();

        var json = await client.GetStringAsync("swagger/spec/swagger.json", TestContext.Current.CancellationToken);
        var root = await client.GetAsync("/", TestContext.Current.CancellationToken);
        var ui = await client.GetStringAsync("swagger/index.html", TestContext.Current.CancellationToken);

        json.Should().Contain("\"openapi\": \"3.2").And.Contain("[Orders] Orders API").And.Contain("https://auth.example.local/connect/token")
            .And.Contain("orders:read");
        root.StatusCode.Should().Be(HttpStatusCode.Redirect);
        root.Headers.Location!.ToString().Should().Be("swagger");
        ui.Should().Contain("<title>[Orders] Orders API</title>").And.Contain("css/swagger-theme.css");
    }

    [Fact]
    public async Task AddToolkitSwagger_DeclaresNoSchemeWithoutAuthSettings()
    {
        await using var app = await StartAsync(new PlainSettings());

        var json = await app.GetTestClient().GetStringAsync("swagger/spec/swagger.json", TestContext.Current.CancellationToken);

        json.Should().NotContain("oauth2").And.Contain("None");
    }

    [Theory]
    [InlineData("css/swagger-colors.css")]
    [InlineData("css/swagger-theme.css")]
    [InlineData("js/swagger-theme.js")]
    public async Task AddToolkitSwagger_ServesTheEmbeddedThemeAssets(string path)
    {
        await using var app = await StartAsync(new PlainSettings());

        var response = await app.GetTestClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
