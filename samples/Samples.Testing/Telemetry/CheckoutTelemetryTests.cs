using Anarchy.Toolkit.Samples.Examples.Telemetry;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Samples.Testing.Telemetry;

/// <summary>
/// Shows <see cref="TestTelemetry"/> through <see cref="TestCaseBase"/>: the code under test receives
/// <c>FixtureTelemetry.Telemetry</c>, and the test asserts on the logs and metrics it captured in memory.
/// </summary>
public class CheckoutTelemetryTests : TestCaseBase
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Checkout_LogsAndCountsTheExecution()
    {
        var service = new CheckoutService(FixtureTelemetry.Telemetry);

        var total = await service.CheckoutAsync("Ada", 42, Ct);

        total.Should().Be(42);
        FixtureTelemetry.Logs.Should().ContainSingle(log => log.Level == LogLevel.Information
                                                            && log.Message.Contains("Checking out Ada for 42"));
        FixtureTelemetry.Metrics.Metrics
            .Should().Contain(metric => metric.Kind == MetricKind.Counter
                                        && metric.Name.EndsWith(".execution_count")
                                        && metric.HasTag("success", true));
    }

    [Fact]
    public async Task Checkout_OverTheLimit_LogsTheDeclineWithItsEventId()
    {
        var service = new CheckoutService(FixtureTelemetry.Telemetry);

        await service.CheckoutAsync("Ada", 2000, Ct);

        FixtureTelemetry.Logs.Should().ContainSingle(log => log.EventId == CheckoutService.PaymentDeclined
                                                            && log.Level == LogLevel.Warning);
        FixtureTelemetry.Metrics.Metrics.Should().Contain(metric => metric.Name.EndsWith(".declined"));
    }
}
