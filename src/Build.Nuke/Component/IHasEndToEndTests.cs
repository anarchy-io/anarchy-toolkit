using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    public interface IHasEndToEndTests : IDefaultBuild
    {
        /// <summary>
        /// Environment variables to be set when executing end-to-end tests on TeamCity. Keys are used verbatim. If the
        /// value is a <see cref="ConfigurationValue"/> (or a string), then the value is also used as-is. If the value
        /// is a <see cref="Secret"/>, then it is decrypted using KMS.
        /// </summary>
        IReadOnlyDictionary<string, ConfigurationValue> EndToEndTestConfiguration { get; }

        /// <summary>
        /// The glob pattern used to determine whether a project is considered an end-to-end test project and should
        /// be run during <see cref="EndToEndTest"/>. By default this
        /// is <c>*.Tests.EndToEnd</c>.
        /// </summary>
        string EndToEndTestProjectNameGlob => "*.Tests.EndToEnd";

        /// <summary>
        /// <para>
        /// A list of all projects considered end-to-end test projects. By default this returns any project in the
        /// entire solution for which <see cref="IsEndToEndTestProject"/> returns <c>true</c>.
        /// </para>
        ///
        /// <para>
        /// <see cref="EndToEndTest"/> executes tests in all of these
        /// projects.
        /// </para>
        /// </summary>
        IReadOnlyCollection<AbsolutePath> EndToEndTestProjects =>
            Solution.AllProjects
                    .Where(IsEndToEndTestProject)
                    .Select(p => p.Path)
                    .ToList();

        /// <summary>
        /// Determines whether the project specified should be considered an end-to-end test project (executed during
        /// <see cref="EndToEndTest"/>). By default, returns
        /// <c>true</c> if the project is a test project (as determined by <see cref="IDefaultBuild.IsTestProject"/>) and the project
        /// name matches <see cref="EndToEndTestProjectNameGlob"/>.
        /// </summary>
        bool IsEndToEndTestProject(Project project) =>
            IsTestProject(project)
         && Glob.IsMatch(project.Name, EndToEndTestProjectNameGlob, GlobOptions.CaseInsensitive);

        /// <summary>
        /// Sets up environment variables needed to run end-to-end tests (the
        /// <see cref="EndToEndTest"/> target) on TeamCity.
        /// </summary>
        Target SetEndToEndTestEnvironmentVariables =>
            _ => _
                .OnlyWhenDynamic(
                     () => IsServerBuild
                 )
                .Unlisted()
                .Executes(() => this.SetEnvironmentVariables(EndToEndTestConfiguration));

        /// <summary>
        /// Runs end-to-end tests. This will always run if invoked explicitly, or if triggered by
        /// <see cref="ICodeDeployBuild.ValidateDeploymentAcceptance"/>
        /// </summary>
        Target EndToEndTest =>
            _ => _
                .TryTriggeredBy<ICodeDeployBuild>(t => t.ValidateDeploymentAcceptance)
                .DependsOn(SetEndToEndTestEnvironmentVariables)
                .Executes(this.RunEndToEndTests);
    }
}
