using Anarchy.Toolkit.Samples.Examples.Orders;
using Anarchy.Toolkit.Samples.Examples.Telemetry;

namespace Anarchy.Toolkit.Samples.Examples;

public static class EntryPointLocal
{
    public static async Task<int> Main(string[] args)
    {
        var examples = new Dictionary<string, Func<TextWriter, Task>>(StringComparer.OrdinalIgnoreCase) {
            ["default"] = DefaultProviderExample.RunAsync,
            ["serilog"] = SerilogConsumerSinkExample.RunAsync,
            ["swap"] = ProviderSwapExample.RunAsync,
            ["recorder"] = CustomMetricRecorderExample.RunAsync,
            ["otlp"] = output => OtlpExportExample.RunAsync(
                new Uri(Environment.GetEnvironmentVariable("OTLP_ENDPOINT") ?? "http://localhost:4318/v1/metrics")),
            ["orders"] = OrdersExample.RunAsync
        };

        var selected = args.FirstOrDefault();
        if (selected is null || !examples.TryGetValue(selected, out var run))
        {
            Console.WriteLine($"Usage: dotnet run --project samples/Samples.Examples -- <{string.Join('|', examples.Keys)}>");
            return 1;
        }

        await run(Console.Out);
        return 0;
    }
}
