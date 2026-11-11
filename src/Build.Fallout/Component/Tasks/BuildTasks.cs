using Fallout.Common.Tools.DotNet;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    /// <summary>
    /// Tasks related to building solutions.
    /// </summary>
    public static class BuildTasks
    {
        /// <summary>
        /// Builds the entire solution in the build specified.
        /// </summary>
        public static void BuildSolution(this IDefaultBuild build) =>
            DotNetTasks.DotNetBuild(
                s => s.SetProjectFile(build.Solution)
                      .SetConfiguration(build.Configuration)
                      .SetTreatWarningsAsErrors(build.TreatWarningsAsErrors)
            );
    }
}
