using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Entity.Build;

/// <summary>
/// Model used to write <c>build.meta.json</c> files.
/// </summary>
internal class BuildMetadataModel
{
    public static string BuildMachine => EnvironmentInfo.MachineName;
    public static string BuiltWhen => DateTime.UtcNow.ToString("o");
    public string? GitSha1 { get; set; }
    public string? BuildNumber { get; set; }
    public string? BuiltBy { get; set; }
}
