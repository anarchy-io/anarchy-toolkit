using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Runtime.Workers.Helpers.OperatingSystems;
using System.Diagnostics;
using System.Reflection;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Health;

/// <summary>
/// Information about the application's environment and build process.
/// </summary>
/// <remarks>
/// Build-related values come from an <see cref="IBuildMetadata"/>; runtime values (machine name, system time,
/// up-time, OS) are evaluated on each access. Serialized by <see cref="ApplicationHealthApi.GetApplicationInformationAsJson"/>.
/// </remarks>
public class ApplicationInformation
{
    internal Func<DateTimeOffset> Clock { get; set; }

    /// <summary>
    /// The name of the hosting assembly.
    /// </summary>
    public string ApplicationName => _buildMetadata.ApplicationName;

    /// <summary>
    /// The local date/time when the application was started.
    /// </summary>
    public DateTimeOffset ApplicationStartTime { get; }

    /// <summary>
    /// The version of the CLR hosting the application.
    /// </summary>
    public string RuntimeVersion => Environment.Version.ToString();

    /// <summary>
    /// The target framework of the running assembly, if available.
    /// </summary>
    public string? TargetFramework => _buildMetadata.TargetFramework;

    /// <summary>
    /// The version of the hosting assembly.
    /// </summary>
    public string ApplicationVersion => _buildMetadata.ApplicationVersion;

    /// <summary>
    /// The Git SHA1 of the head commit at the time of building.
    /// </summary>
    public string? BuildCommitHash => _buildMetadata.BuildCommitHash;

    /// <summary>
    /// The machine name of the system the application was built on.
    /// </summary>
    public string? BuildMachineName => _buildMetadata.BuildMachineName;

    /// <summary>
    /// The local date/time the application was built.
    /// </summary>
    public DateTimeOffset? BuildTimestamp => _buildMetadata.BuildTimestamp;

    /// <summary>
    /// The Git user that triggered the build.
    /// </summary>
    public string? BuildUsername => _buildMetadata.BuildUsername;

    /// <summary>
    /// The CI build number.
    /// </summary>
    public string? BuildNumber => _buildMetadata.BuildNumber;

    /// <summary>
    /// The host name of the machine the application is running on.
    /// </summary>
    public string MachineName => Environment.MachineName;

    /// <summary>
    /// A URI pointing to a 'runbook' that operations teams can use to diagnose problems with your application.
    /// </summary>
    public string? RunbookUri => _buildMetadata.RunbookUri;

    /// <summary>
    /// The current local date/time of the application's environment.
    /// </summary>
    public DateTimeOffset SystemTime => Clock();

    /// <summary>
    /// The application's up-time, based on its start time and the current system time.
    /// </summary>
    public TimeSpan Uptime => SystemTime.Subtract(ApplicationStartTime);

    /// <summary>
    /// The operating system the app is currently running on
    /// </summary>
    public string OperatingSystem => OperatingSystemInfo.Current;

    /// <summary>
    /// Application information, that reads build metadata. The application start time is the start time of the
    /// current process, so it does not depend on when the container first resolves this instance.
    /// </summary>
    /// <param name="buildMetadata">The build metadata provider.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="buildMetadata"/> is <see langword="null"/>.</exception>
    public ApplicationInformation(
        IBuildMetadata buildMetadata
    )
        : this(buildMetadata, new DateTimeOffset(Process.GetCurrentProcess().StartTime))
    {
    }

    private ApplicationInformation(
        IBuildMetadata buildMetadata,
        DateTimeOffset applicationStartTime
    )
    {
        _buildMetadata = buildMetadata ?? throw new ArgumentNullException(nameof(buildMetadata));
        ApplicationStartTime = applicationStartTime;

        Clock = () => DateTimeOffset.Now;
    }

    /// <summary>
    /// Create a new <see cref="ApplicationInformation" />, reading build metadata embedded in the specified
    /// assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded build metadata.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    /// <returns>The new application information.</returns>
    public static ApplicationInformation WithMetadataFromAssembly(
        Assembly assembly,
        DateTimeOffset applicationStartTime
    )
    {
        var buildMetadata = new BuildMetadataFromEmbeddedResource(assembly);

        return new ApplicationInformation(buildMetadata, applicationStartTime);
    }

    /// <summary>
    /// Create a new <see cref="ApplicationInformation" /> with a custom build metadata provider.
    /// </summary>
    /// <param name="buildMetadata">The build metadata provider.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    /// <returns>The new application information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="buildMetadata"/> is <see langword="null"/>.</exception>
    public static ApplicationInformation WithCustomMetadata(
        IBuildMetadata buildMetadata,
        DateTimeOffset applicationStartTime
    )
    {
        return new ApplicationInformation(buildMetadata, applicationStartTime);
    }

    private readonly IBuildMetadata _buildMetadata;
}
