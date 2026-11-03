using Nuke.Common.Execution;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    /// <summary>
    /// Opts out from NUKE telemetry
    /// </summary>
    public sealed class TelemetryOptoutAttribute : BuildExtensionAttributeBase, IOnBuildCreated
    {
        public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            System.Environment.SetEnvironmentVariable("NUKE_TELEMETRY_OPTOUT", "1");
        }
    }
}
