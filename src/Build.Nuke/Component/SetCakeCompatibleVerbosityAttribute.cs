using Nuke.Common;
using Nuke.Common.Execution;
using Logger = Anarchy.Toolkit.Build.Nuke.Infrastructure.Helper.Logger;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    /// <summary>
    /// Correctly configures log levels when running from TeamCity, since log levels and verbosity
    /// are slightly different from Cake.
    /// </summary>
    public sealed class SetCakeCompatibleVerbosityAttribute : BuildExtensionAttributeBase, IOnBuildCreated
    {
        public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            // by default TC builds specify Minimal verbosity, but Minimal verbosity for Cake is Normal verbosity for Nuke
            if (!NukeBuild.IsServerBuild || NukeBuild.Verbosity < Verbosity.Minimal)
                return;

            NukeBuild.Verbosity = Verbosity.Normal;
            Logger.Info("Because Minimal verbosity was specified; overrode it to {0}", NukeBuild.Verbosity);
        }
    }
}
