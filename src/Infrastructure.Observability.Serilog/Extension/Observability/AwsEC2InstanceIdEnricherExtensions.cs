using Serilog;
using Serilog.Configuration;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Observability.Serilog.Extension.Observability;

public static class AwsEc2InstanceIdEnricherExtensions
{
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
