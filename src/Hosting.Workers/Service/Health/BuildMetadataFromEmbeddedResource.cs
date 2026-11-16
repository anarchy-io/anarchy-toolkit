using Anarchy.Toolkit.Core.Health.Entity.Attribute;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Entity.Service.Health;
using System.Reflection;
using System.Runtime.Versioning;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health;

/// <summary>
/// Reads build metadata from an assembly-embedded resource in JSON format.
/// </summary>
/// <remarks>
/// <para>The resource is <c>{AssemblyName}.Properties.build.meta.json</c> (dashes in the assembly name replaced by
/// underscores), with the properties <c>BuildMachine</c>, <c>BuiltBy</c>, <c>BuiltWhen</c>, <c>GitSha1</c> and
/// <c>BuildNumber</c>. When missing, those values read <c>"build.meta.json not found"</c>.</para>
/// <para>Application name and version come from the assembly name, the target framework from its
/// <see cref="TargetFrameworkAttribute"/>, and the runbook URI from
/// <see cref="ApplicationRunbookUriAttribute"/>. Everything is read once, in the constructor.</para>
/// </remarks>
public class BuildMetadataFromEmbeddedResource : IBuildMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="BuildMetadataFromEmbeddedResource"/> with the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to read the metadata from.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <see langword="null"/>.</exception>
    public BuildMetadataFromEmbeddedResource(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        ApplicationName = assembly.GetName().Name!;
        ApplicationVersion = assembly.GetName().Version!.ToString();
        TargetFramework = GetTargetFramework(assembly);

        var metadata = ReadMetadata(assembly);

        BuildCommitHash = metadata.GitSha1;
        BuildMachineName = metadata.BuildMachine;
        BuildNumber = metadata.BuildNumber;
        BuildTimestamp = metadata.BuiltWhen;
        BuildUsername = metadata.BuiltBy;

        RunbookUri = GetRunbookUri(assembly);
    }

    private static string? GetTargetFramework(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        return attribute?.FrameworkName;
    }

    /// <inheritdoc />
    public string ApplicationName { get; }

    /// <inheritdoc />
    public string ApplicationVersion { get; }

    /// <inheritdoc />
    public string? TargetFramework { get; }

    /// <inheritdoc />
    public string? BuildNumber { get; }

    /// <inheritdoc />
    public string? BuildMachineName { get; }

    /// <inheritdoc />
    public string? BuildUsername { get; }

    /// <inheritdoc />
    public DateTimeOffset? BuildTimestamp { get; }

    /// <inheritdoc />
    public string? BuildCommitHash { get; }

    /// <inheritdoc />
    public string? RunbookUri { get; }

    private static BuildMetadata ReadMetadata(Assembly assembly)
    {
        var reader = new BuildMetadataReader();
        return BuildMetadataReader.ReadMetadata(assembly);
    }

    private static string? GetRunbookUri(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<ApplicationRunbookUriAttribute>();

        return attribute?.Uri;
    }
}
