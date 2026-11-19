using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Optional interactive NuGet dependency maintenance.
/// </summary>
public interface IDependencyUpgradeBuild : IStandardBuild
{
    /// <summary>
    /// How far packages may be upgraded (<c>--dependency-version-lock</c>): <c>Major</c> (the default) keeps the major
    /// version, <c>Minor</c> keeps the minor version, <c>None</c> allows any version. Case-insensitive.
    /// </summary>
    [Parameter("Dependency version lock: Major, Minor or None")]
    string DependencyVersionLock => TryGetValue(() => DependencyVersionLock) ?? "Major";

    /// <summary>
    /// Whether to install or reinstall the global <c>dotnet-outdated-tool</c> without asking
    /// (<c>--dependency-install-tool</c>). Defaults to <see langword="false"/>.
    /// </summary>
    [Parameter("Allow installing or reinstalling dotnet-outdated without prompting")]
    bool DependencyInstallTool => TryGetValue<bool?>(() => DependencyInstallTool) ?? false;

    /// <summary>
    /// Whether to upgrade the outdated dependencies without asking (<c>--dependency-upgrade</c>). Defaults to
    /// <see langword="false"/>.
    /// </summary>
    [Parameter("Allow upgrading dependencies without prompting")]
    bool DependencyUpgrade => TryGetValue<bool?>(() => DependencyUpgrade) ?? false;

    /// <summary>
    /// Lists outdated packages, then upgrades them with <c>dotnet-outdated</c> (including centrally pinned transitive
    /// packages in <c>Directory.Packages.props</c>) and restores. Confirmations not granted by
    /// <see cref="DependencyInstallTool"/> or <see cref="DependencyUpgrade"/> are asked interactively, and answered no on
    /// CI, on a server build or with redirected input.
    /// </summary>
    Target UpgradeDependencies => d => d.Description("List and optionally upgrade outdated NuGet packages")
        .Executes(() => DependencyUpgradeTasks.Run(RootDirectory, Solution.Path, DependencyVersionLock,
            DependencyInstallTool, DependencyUpgrade, !IsServerBuild && !IsCiBuild && !Console.IsInputRedirected));
}
