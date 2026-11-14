using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.AspNetCore;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mime;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health.Middleware;

public class AspNetCoreBaseTests : IAsyncLifetime
{
    protected async Task AssertUrlResponseAsync(
        string url,
        HttpMethod verb,
        HttpStatusCode expectedStatusCode,
        string? expectedContentType,
        Action<string>? contentAssertion
    )
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new HttpRequestMessage(verb, url);
        using var response = await _server!.CreateClient().SendAsync(request, cancellationToken);

        response.StatusCode.Should().Be(expectedStatusCode);

        var contentType = response.Content.Headers.ContentType?.ToString();
        contentType.Should().Be(expectedContentType);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        contentAssertion?.Invoke(content);
    }

    protected Task CreateDefaultTestServer() => CreateTestServer<DefaultTestServerStartup>();

    protected async Task CreateTestServer<T>()
        where T : class
    {
        if (_host is not null)
        {
            await StopAndDisposeHost();
        }

        _host = await Host.CreateDefaultBuilder(Array.Empty<string>())
                          .ConfigureLogging(logging => logging.ClearProviders())
                          .ConfigureWebHostDefaults(b => {
                              b.UseEnvironment("Development");
                              b.UseStartup<T>();
                              b.UseTestServer();
                          })
                          .StartAsync(TestContext.Current.CancellationToken);

        _server = _host.GetTestServer();
    }

    private IHost? _host;
    private TestServer? _server;
    protected const string OnlyGetRequestsAllowedMessage = "Only GET requests are allowed.";
    protected const string DefaultResponseText = "Default response";

    public async ValueTask InitializeAsync() => await CreateDefaultTestServer();

    public async ValueTask DisposeAsync()
    {
        await StopAndDisposeHost();
        GC.SuppressFinalize(this);
    }

    private async Task StopAndDisposeHost()
    {
        if (_server is not null)
        {
            _server.Dispose();
            _server = null;
        }

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }
    }

    public class DefaultTestServerStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);

            base.ConfigureServices(services);
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseApplicationHealthEndpoints();

            base.Configure(app);
        }
    }

    public abstract class StartupBase
    {
        public const string DefaultResponseText = "Default response";

        public virtual void ConfigureServices(IServiceCollection services)
        {
        }

        public virtual void Configure(IApplicationBuilder app)
        {
            app.Run(
                context => {
                    context.Response.ContentType = MediaTypeNames.Text.Plain;
                    return context.Response.WriteAsync(DefaultResponseText);
                }
            );
        }
    }
}
