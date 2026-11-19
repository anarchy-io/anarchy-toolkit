using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Http.Repository.Http;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Http.Repository.Http;

public class HttpRepositoryBaseTest
{
    [Fact]
    public async Task GetAsync_UsesInjectedTelemetryAndReturnsTheResponse()
    {
        var token = TestContext.Current.CancellationToken;
        var telemetry = new Mock<IAppTelemetry>();
        telemetry.Setup(t => t.MonitoredActionAsync(It.IsAny<CallSite>(), It.IsAny<Func<Task<HttpResponseMessage>>>(), token))
            .Returns((CallSite _, Func<Task<HttpResponseMessage>> action, CancellationToken _) => action());
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("orders")).Returns(() => new HttpClient(new Handler()) { BaseAddress = new Uri("https://example.test/") });
        var repository = new Repository(telemetry.Object, factory.Object);

        using var response = await repository.GetAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        telemetry.Verify(t => t.MonitoredActionAsync(It.Is<CallSite>(site => site.MethodName == "MonitoredGetAsync"),
            It.IsAny<Func<Task<HttpResponseMessage>>>(), token), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenRequestFails_EmitsTelemetryToTheConsumerSink()
    {
        var token = TestContext.Current.CancellationToken;
        var sink = new InMemorySink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.AddTelemetryLogMetrics();
        using var provider = services.BuildServiceProvider();
        var listener = provider.GetServices<IHostedService>().Should().ContainSingle().Which;
        await listener.StartAsync(token);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("orders")).Returns(() => new HttpClient(new FailingHandler()) { BaseAddress = new Uri("https://example.test/") });
        var repository = new Repository(provider.GetRequiredService<IAppTelemetry>(), factory.Object);

        var action = () => repository.GetAsync(token);
        await action.Should().ThrowAsync<HttpRequestException>();

        sink.Messages.Should().Contain(message => message.RenderMessage().Contains("Service action failed"));
        sink.Messages.Should().Contain(message => message.RenderMessage().Contains("monitored_get_async.execution_count"));
        await listener.StopAsync(token);
    }

    private sealed class Repository(IAppTelemetry telemetry, IHttpClientFactory factory) : HttpRepositoryBase(telemetry, factory)
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
