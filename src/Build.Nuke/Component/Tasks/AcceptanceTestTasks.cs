using Anarchy.Toolkit.Build.Nuke.Infrastructure.Helper;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    /// <summary>
    /// Tasks related to the running of acceptance tests.
    /// </summary>
    public static class AcceptanceTestTasks
    {
        /// <summary>
        /// Runs acceptance tests. This runs all projects defined in <see cref="IHasAcceptanceTests.AcceptanceTestProjects"/>,
        /// using a TeamCity-compatible logger (see <see cref="TeamCityLoggingHelper.ConfigureTeamCityCompatibleTestVerbosity(DotNetTestSettings)"/>).
        /// Projects built on Microsoft.Testing.Platform are run through it directly (see
        /// <see cref="TestingPlatformHelper"/>), all others through <c>dotnet test</c>. This method is called by
        /// <see cref="IHasAcceptanceTests.AcceptanceTest"/>.
        /// </summary>
        public static void RunAcceptanceTests(this IHasAcceptanceTests build)
        {
            var (testingPlatformProjects, vsTestProjects) =
                TestingPlatformHelper.PartitionByRunner(build.Solution, build.AcceptanceTestProjects);

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
