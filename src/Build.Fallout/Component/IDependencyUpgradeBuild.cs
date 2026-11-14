using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// Optional interactive NuGet dependency maintenance.
/// </summary>
public interface IDependencyUpgradeBuild : IStandardBuild
{
    [Parameter("Dependency version lock: Major, Minor or None")]
    string DependencyVersionLock => TryGetValue(() => DependencyVersionLock) ?? "Major";

    [Parameter("Allow installing or reinstalling dotnet-outdated without prompting")]
    bool DependencyInstallTool => TryGetValue<bool?>(() => DependencyInstallTool) ?? false;

    [Parameter("Allow upgrading dependencies without prompting")]
    bool DependencyUpgrade => TryGetValue<bool?>(() => DependencyUpgrade) ?? false;

    Target UpgradeDependencies => d => d.Description("List and optionally upgrade outdated NuGet packages")
        .Executes(() => DependencyUpgradeTasks.Run(RootDirectory, Solution.Path, DependencyVersionLock,
            DependencyInstallTool, DependencyUpgrade, !IsServerBuild && !IsCiBuild && !Console.IsInputRedirected));
}
