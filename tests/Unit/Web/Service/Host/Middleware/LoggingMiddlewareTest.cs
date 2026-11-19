using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Web.Service.Host.Middleware;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Web.Service.Host.Middleware;

public class LoggingMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_EmitsRequestLogsAndMetricsToTheConsumerSink()
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
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/orders";
        var middleware = new LoggingMiddleware(httpContext => {
            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        }, provider.GetRequiredService<IAppTelemetry>());

        await middleware.InvokeAsync(context);

        sink.Messages.Should().Contain(entry => entry.MessageTemplate.Text.Contains("Finished {Method} request"));
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("execution_time"));
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("request_method, POST"));
        await listener.StopAsync(token);
    }
}
