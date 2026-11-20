using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Http.Repository.Http;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Http.Repository.Http;

public class HttpRepositoryBaseTest
{
    [Fact]
    public async Task GetAsync_MonitorsTheRequestAndReturnsTheResponse()
    {
        var token = TestContext.Current.CancellationToken;
        using var telemetry = new TestTelemetry();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("orders")).Returns(() => new HttpClient(new Handler()) { BaseAddress = new Uri("https://example.test/") });
        var repository = new Repository(telemetry.Telemetry, factory.Object);

        using var response = await repository.GetAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        telemetry.Metrics.CounterTotal("repository.monitored_get_async.execution_count").Should().Be(1);
        telemetry.Metrics.Named("repository.monitored_get_async.execution_time").Should().ContainSingle()
            .Which.HasTag("success", true).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WhenRequestFails_LogsTheFailureAndCountsIt()
    {
        var token = TestContext.Current.CancellationToken;
        using var telemetry = new TestTelemetry();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("orders")).Returns(() => new HttpClient(new FailingHandler()) { BaseAddress = new Uri("https://example.test/") });
        var repository = new Repository(telemetry.Telemetry, factory.Object);

        var action = () => repository.GetAsync(token);
        await action.Should().ThrowAsync<HttpRequestException>();

        telemetry.Logs.Should().Contain(log => log.Level == LogLevel.Error && log.Message.Contains("Service action failed"));
        telemetry.Metrics.Named("repository.monitored_get_async.execution_count").Should().ContainSingle()
            .Which.HasTag("success", false).Should().BeTrue();
    }

    private sealed class Repository(AppTelemetry telemetry, IHttpClientFactory factory) : HttpRepositoryBase(telemetry, factory)
    {
        protected override string GetClientName() => "orders";
        public Task<HttpResponseMessage> GetAsync(CancellationToken token) => MonitoredGetAsync("orders", token);
    }

    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("failed"));
    }
}
