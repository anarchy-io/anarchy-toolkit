using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    public interface IHasAcceptanceTests : IDefaultBuild
    {
        /// <summary>
        /// Environment variables to be set when executing acceptance tests on TeamCity. Keys are used verbatim. If the
        /// value is a <see cref="ConfigurationValue"/> (or a string), then the value is also used as-is. If the value
        /// is a <see cref="Secret"/>, then it is decrypted using KMS.
        /// </summary>
        IReadOnlyDictionary<string, ConfigurationValue> AcceptanceTestConfiguration { get; }

        /// <summary>
        /// The glob pattern used to determine whether a project is considered an acceptance test project and should
        /// be run during <see cref="AcceptanceTest"/>. By default this
        /// is <c>*.Tests.Acceptance</c>.
        /// </summary>
        string AcceptanceTestProjectNameGlob => "*.Tests.Acceptance";

        /// <summary>
        /// <para>
        /// A list of all projects considered acceptance test projects. By default this returns any project in the
        /// entire solution for which <see cref="IsAcceptanceTestProject"/> returns <c>true</c>.
        /// </para>
        ///
        /// <para>
        /// <see cref="AcceptanceTest"/> executes tests in all of these
        /// projects.
        /// </para>
        /// </summary>
        IReadOnlyCollection<AbsolutePath> AcceptanceTestProjects =>
            Solution.AllProjects
                    .Where(IsAcceptanceTestProject)
                    .Select(p => p.Path)
                    .ToList();

        /// <summary>
        /// Determines whether the project specified should be considered an acceptance test project (executed during
        /// <see cref="AcceptanceTest"/>). By default, returns
        /// <c>true</c> if the project is a test project (as determined by <see cref="IDefaultBuild.IsTestProject"/>) and the project
        /// name matches <see cref="AcceptanceTestProjectNameGlob"/>.
        /// </summary>
        bool IsAcceptanceTestProject(Project project) =>
            IsTestProject(project)
         && Glob.IsMatch(project.Name, AcceptanceTestProjectNameGlob, GlobOptions.CaseInsensitive);

        /// <summary>
        /// Sets up environment variables needed to run acceptance tests (the
        /// <see cref="AcceptanceTest"/> target) on TeamCity.
        /// </summary>
        Target SetAcceptanceTestEnvironmentVariables =>
            _ => _
                .OnlyWhenDynamic(
                     () => IsServerBuild
                 )
                .Unlisted()
                .Executes(() => this.SetEnvironmentVariables(AcceptanceTestConfiguration));

        /// <summary>
        /// Runs acceptance tests. This will always run if invoked explicitly, or if triggered by
        /// <see cref="ICodeDeployBuild.ValidateDeploymentAcceptance"/>
        /// </summary>
        Target AcceptanceTest =>
            _ => _
                .TryTriggeredBy<ICodeDeployBuild>(t => t.ValidateDeploymentAcceptance)
                .DependsOn(SetAcceptanceTestEnvironmentVariables)
                .Executes(this.RunAcceptanceTests);
    }
}
