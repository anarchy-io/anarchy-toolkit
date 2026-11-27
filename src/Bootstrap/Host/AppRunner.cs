namespace Anarchy.Toolkit.Bootstrap.Host;

/// <summary>
/// Describes how the application is run. When registered in DI, it lets composition code (for example an application's
/// telemetry module) adapt to the runtime.
/// </summary>
/// <param name="Type">The runner type.</param>
public record AppRunner(AppRunner.Types Type)
{
    /// <summary>
    /// The supported application runners.
    /// </summary>
    public enum Types
    {
        /// <summary>The application runs as a short-lived job (e.g. a local run or a serverless function invocation).</summary>
        Job,

        /// <summary>The application runs as a long-lived worker (e.g. on EC2).</summary>
        Ec2Worker
    }
}
