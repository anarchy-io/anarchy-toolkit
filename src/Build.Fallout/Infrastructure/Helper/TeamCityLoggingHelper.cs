using Fallout.Common;
using Fallout.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper
{
    /// <summary>
    /// Helper functions to ‘fix’ logging to TeamCity.
    /// </summary>
    public static class TeamCityLoggingHelper
    {
        /// <summary>
        /// Sets the log level of a <see cref="DotNetTestSettings"/> instance to one compatible with TeamCity test reporting.
        /// This means the verbosity will never be set lower than <see cref="DotNetVerbosity.normal"/>.
        /// </summary>
        public static DotNetTestSettings ConfigureTeamCityCompatibleTestVerbosity(DotNetTestSettings settings) =>
            FalloutBuild.Verbosity switch {
                Verbosity.Verbose => settings.SetVerbosity(DotNetVerbosity.detailed),
                _ => settings.SetVerbosity(DotNetVerbosity.normal),
            };

        /// <summary>
        /// Sets the log level of a <see cref="DotNetMSBuildSettings"/> instance used to run tests through
        /// Microsoft.Testing.Platform. MSBuild only needs to relay what the test application writes, so anything
        /// above <see cref="DotNetVerbosity.minimal"/> just buries the test results in target logging.
        /// </summary>
        public static DotNetMSBuildSettings ConfigureTeamCityCompatibleTestVerbosity(DotNetMSBuildSettings settings) =>
            FalloutBuild.Verbosity switch {
                Verbosity.Verbose => settings.SetVerbosity(DotNetVerbosity.detailed),
                _ => settings.SetVerbosity(DotNetVerbosity.minimal),
            };
    }
}
