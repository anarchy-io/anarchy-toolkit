using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>
/// The instrumented code every telemetry example runs. It only knows <see cref="AppTelemetry"/>: where its logs and
/// metrics end up is decided by the composition root, so none of the examples changes this class.
/// </summary>
public sealed class CheckoutService(AppTelemetry telemetry)
{
    public static readonly EventId PaymentDeclined = new(2001, "payment.declined");

    public Task<decimal> CheckoutAsync(string customer, decimal total, CancellationToken cancellationToken = default) =>
        telemetry.MonitoredActionAsync(this.CalledFrom(), () => {
            telemetry.LogInformation(this.CalledFrom(), "Checking out {Customer} for {Total}", customer, total);
            telemetry.RecordHistogram(this.CalledFrom(), "total", (double)total, new KeyValuePair<string, object?>("currency", "EUR"));

            if (total > 1000)
            {
                telemetry.Log(LogLevel.Warning, this.CalledFrom(), PaymentDeclined, null,
                              "Payment declined for {Customer}", customer);
                telemetry.IncrementCounter(this.CalledFrom(), "declined");
            }

            return Task.FromResult(total);
        }, cancellationToken);
}
