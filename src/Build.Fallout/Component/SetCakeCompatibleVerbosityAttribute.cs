using Fallout.Common;
using Fallout.Common.Execution;
using Logger = Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper.Logger;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    /// <summary>
    /// Correctly configures log levels when running from TeamCity, since log levels and verbosity
    /// are slightly different from Cake.
    /// </summary>
    public sealed class SetCakeCompatibleVerbosityAttribute : BuildExtensionAttributeBase, IOnBuildCreated
    {
        public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            // TeamCity specifies Minimal verbosity, but Cake's Minimal corresponds to Fallout's Normal.
            if (!FalloutBuild.IsServerBuild || FalloutBuild.Verbosity < Verbosity.Minimal)
                return;

            FalloutBuild.Verbosity = Verbosity.Normal;
            Logger.Info("Because Minimal verbosity was specified; overrode it to {0}", FalloutBuild.Verbosity);
        }
    }
}
