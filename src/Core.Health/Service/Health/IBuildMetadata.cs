namespace Anarchy.Toolkit.Core.Health.Service.Health;

/// <summary>
/// Represents application-level metadata determined at build-time.
/// </summary>
public interface IBuildMetadata
{
    /// <summary>
    /// The name of the specified assembly.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// The version of the specified assembly.
    /// </summary>
    string ApplicationVersion { get; }

    /// <summary>
    /// The target framework of the running assembly, if available.
    /// </summary>
    string? TargetFramework { get; }

    /// <summary>
    /// The CI build number.
    /// </summary>
    string? BuildNumber { get; }

    /// <summary>
    /// The machine name of the system the application was built on.
    /// </summary>
    string? BuildMachineName { get; }

    /// <summary>
    /// The Git user that triggered the build.
    /// </summary>
    string? BuildUsername { get; }

    /// <summary>
    /// The local date/time the application was built.
    /// </summary>
    DateTimeOffset? BuildTimestamp { get; }

    /// <summary>
    /// The Git SHA1 of the head commit at the time of building.
    /// </summary>
    string? BuildCommitHash { get; }

    /// <summary>
    /// A URI pointing to a 'runbook' that operations teams can use to diagnose problems with your application.
    /// </summary>
    string? RunbookUri { get; }
}
