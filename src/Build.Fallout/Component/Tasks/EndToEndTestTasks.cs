using Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    /// <summary>
    /// Tasks related to the running of end-to-end tests.
    /// </summary>
    public static class EndToEndTestTasks
    {
        /// <summary>
        /// Runs end-to-end tests. This runs all projects defined in <see cref="IHasEndToEndTests.EndToEndTestProjects"/>,
        /// using a TeamCity-compatible logger (see <see cref="TeamCityLoggingHelper.ConfigureTeamCityCompatibleTestVerbosity(DotNetTestSettings)"/>).
        /// Projects built on Microsoft.Testing.Platform are run through it directly (see
        /// <see cref="TestingPlatformHelper"/>), all others through <c>dotnet test</c>. This method is called by
        /// <see cref="IHasEndToEndTests.EndToEndTest"/>.
        /// </summary>
        public static void RunEndToEndTests(this IHasEndToEndTests build)
        {
            var (testingPlatformProjects, vsTestProjects) =
                TestingPlatformHelper.PartitionByRunner(build.Solution, build.EndToEndTestProjects);

            foreach (var projectFile in testingPlatformProjects)
            {
                TestingPlatformHelper.RunTests(projectFile, build.Configuration);
            }

            DotNetTasks.DotNetTest(
                s => TeamCityLoggingHelper
                     .ConfigureTeamCityCompatibleTestVerbosity(s)
                     .SetConfiguration(build.Configuration)
                     .CombineWith(
                          c => vsTestProjects.Select(p => c.SetProjectFile(p))
                      )
            );
        }
    }
}
