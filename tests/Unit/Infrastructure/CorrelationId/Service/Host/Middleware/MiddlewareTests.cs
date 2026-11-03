using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Service.Builder;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.CorrelationId.Service.Host.Middleware;

public class MiddlewareTests : IDisposable
{
    private IHost _host = default!;
    private TestServer _testServer = default!;

    private void CreateTestServer(CorrelationIdOptions? options = null)
    {
        _host = new HostBuilder()
           .ConfigureWebHost(webBuilder => webBuilder
               .UseTestServer()
               .ConfigureServices(services => services.AddCorrelationId(options ?? new CorrelationIdOptions()))
               .Configure(app => app.UseCorrelationId()))
           .Start();

        _testServer = _host.GetTestServer();
    }

    [Fact]
    public async Task CorrelationMiddleware_GeneratesHeadersAsDefault()
    {
        CreateTestServer();

        var response =
            await _testServer.CreateRequest("/").GetAsync();

        AssertHasNonEmptyCorrelationHeaders(response);
    }

    [Fact]
    public async Task CorrelationMiddleware_DoesNothingIfNoHeaders()
    {
        CorrelationIdOptions options = new() { Behavior = CorrelationIdBehavior.IgnoreIfMissing };
        CreateTestServer(options);

        var response =
            await _testServer.CreateRequest("/").GetAsync();

        response.Headers.Should().NotContainKey(CorrelationHeaderNames.CorrelationId)
                .And.NotContainKey(CorrelationHeaderNames.AttemptId);
    }

    [Fact]
    public async Task CorrelationMiddleware_AutoGeneratesCorrelationIdAndAttemptId()
    {
        CorrelationIdOptions options = new() { Behavior = CorrelationIdBehavior.GenerateValues };
        CreateTestServer(options);

        var response =
            await _testServer.CreateRequest("/").GetAsync();

        AssertHasNonEmptyCorrelationHeaders(response);
    }

    [Fact]
    public async Task CorrelationMiddleware_ThrowsInvalidOperationWhenBehaviorSet()
    {
        CorrelationIdOptions options = new() { Behavior = CorrelationIdBehavior.ThrowIfMissing };
        CreateTestServer(options);

        await _testServer.Invoking(x => x.CreateRequest("/").GetAsync())
                        .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CorrelationMiddleware_AddsCorrelationIdAndAttemptIdToResponse()
    {
        CreateTestServer();
        var correlationId = Guid.NewGuid().ToString();
        var attemptId = Guid.NewGuid().ToString();
        var response =
            await _testServer.CreateRequest("/")
                            .AddHeader(name: CorrelationHeaderNames.CorrelationId, value: correlationId)
                            .AddHeader(name: CorrelationHeaderNames.AttemptId, value: attemptId)
                            .GetAsync();

        using (new AssertionScope())
        {
            response.Headers.Should().ContainKey(CorrelationHeaderNames.CorrelationId)
                    .WhoseValue.Single().Should().Be(correlationId);
            response.Headers.Should().ContainKey(CorrelationHeaderNames.AttemptId)
                    .WhoseValue.Single().Should().Be(attemptId);
        }
    }

    private static void AssertHasNonEmptyCorrelationHeaders(HttpResponseMessage response)
    {
        using (new AssertionScope())
        {
            response.Headers.Should().ContainKey(CorrelationHeaderNames.CorrelationId)
                    .WhoseValue.Single().Should().HaveLength(36)
                    .And.NotBeEquivalentTo(Guid.Empty.ToString());
            response.Headers.Should().ContainKey(CorrelationHeaderNames.AttemptId)
                    .WhoseValue.Single().Should().HaveLength(36)
                    .And.NotBeEquivalentTo(Guid.Empty.ToString());
        }
    }

    public void Dispose() => _host?.Dispose();
}
