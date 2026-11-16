using Serilog;
using Serilog.Configuration;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Observability.Serilog.Extension.Observability;

/// <summary>
/// Serilog enrichment extensions for applications hosted on AWS EC2.
/// </summary>
public static class AwsEc2InstanceIdEnricherExtensions
{
    /// <summary>
    /// Enriches all log events with an <c>InstanceId</c> property holding the EC2 instance id.
    /// </summary>
    /// <remarks>
    /// The id is resolved once, when this method is called, by running <c>ec2-metadata -i</c> through
    /// <c>/bin/bash</c> (blocking until it exits). On non-Unix platforms, or when the command is unavailable or its
    /// output is unexpected, no property is added; failures are written to the console and never thrown.
    /// </remarks>
    /// <param name="cfg">The logger enrichment configuration.</param>
    /// <returns>The <see cref="LoggerConfiguration"/>, to allow chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// new LoggerConfiguration().Enrich.WithAwsEc2InstanceId();
    /// </code>
    /// </example>
    public static LoggerConfiguration WithAwsEc2InstanceId(
        this LoggerEnrichmentConfiguration cfg)
    {
        var instanceId = GetInstanceId();

        return instanceId != null ? cfg.WithProperty("InstanceId", instanceId) : cfg.With();
    }

    private static string? GetInstanceId()
    {
        try
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                return null;

            var psi = new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = "ec2-metadata -i",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
                return null;

            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd();

            if (output.StartsWith("instance-id: "))
            {
                var values = output.Split(" ");
                if (values.Length == 2)
                    return values[1];
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to get instance id: {e.Message}");
        }

        return null;
    }
}
