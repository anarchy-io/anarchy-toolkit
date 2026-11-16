namespace Anarchy.Toolkit.Configuration.Host;

/// <summary>
/// Describes how the application is run. When registered in DI, it lets observability adapt to the runtime (for
/// example, <see cref="Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service.ObserverFactory"/>
/// adds the AWS request context enricher on <see cref="Types.Lambda"/>).
/// </summary>
/// <param name="Type">The runner type.</param>
public record AppRunner(AppRunner.Types Type)
{
    /// <summary>
    /// The supported application runners.
    /// </summary>
    public enum Types
    {
        /// <summary>The application runs as an AWS Lambda function.</summary>
        Lambda,

        /// <summary>The application runs as a long-lived worker (e.g. on EC2).</summary>
        Ec2Worker
    }
}
