using Anarchy.Toolkit.Testing.Telemetry;
using Anarchy.Toolkit.Web.Service.Host.Middleware;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Tests.Unit.Web.Service.Host.Middleware;

public class LoggingMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_EmitsRequestLogsAndMetricsThroughTheInjectedTelemetry()
    {
        using var telemetry = new TestTelemetry();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/orders";
        var middleware = new LoggingMiddleware(httpContext => {
            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        }, telemetry.Telemetry);

        await middleware.InvokeAsync(context);

        telemetry.Logs.Should().Contain(log => log.MessageTemplate!.Contains("Finished {Method} request"));
        telemetry.Metrics.Metrics.Should().ContainSingle(metric => metric.Kind == MetricKind.Duration
                                                                 && metric.Name == "logging_middleware.invoke_async.execution_time")
            .Which.Should().Match<RecordedMetric>(metric => metric.HasTag("request_method", "POST")
                                                            && metric.HasTag("status_code", "201")
                                                            && metric.HasTag("success", true));
    }
}
