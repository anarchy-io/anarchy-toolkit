using Anarchy.Toolkit.Build.Fallout.Infrastructure.Entity.UserSecrets;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.DotNetUserSecrets;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Logger = Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper.Logger;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    internal static class UserSecretsTasks
    {
        public static void SetUserSecrets(this IDefaultBuild build)
        {
            // call build.UserSecretsProjects once here and store in a variable,
            // in case the property is an expression body returning a new collection
            // each time its called, losing prompts internal state
            var userSecretsProjects = build.UserSecretsProjects;

            if (!userSecretsProjects.Any())
            {
                LogNoUserSecretsProjects();
                return;
            }

            foreach (var promptedUserSecret in userSecretsProjects.SelectMany(usp => usp.UserSecrets.Values.OfType<IPromptedUserSecretValue>()).Distinct())
                promptedUserSecret.Prompt();

            foreach (var userSecretsProject in userSecretsProjects)
            {
                if (!userSecretsProject.UserSecrets.Any())
                {
                    LogNoUserSecretsForProject(userSecretsProject.ProjectFile);
                    continue;
                }

                foreach (var userSecret in userSecretsProject.UserSecrets)
                {
                    DotNetUserSecretsTasks.DotNetUserSecretsSet(
                        s => s.SetProject(userSecretsProject.ProjectFile)
                              .SetKey(userSecret.Key)
                              .SetValue(userSecret.Value.Value)
                              .DisableProcessInvocationLogging()
                              .DisableProcessOutputLogging()
                    );

                    LogUserSecretSaved(userSecretsProject.ProjectFile, userSecret);
                }
            }
        }

        static void LogUserSecretSaved(AbsolutePath projectFile, KeyValuePair<string, DotNetUserSecretValue> userSecret)
        {
            Logger.Info($"Successfully saved {userSecret.Key} = {userSecret.Value.LogValue} to the secret store of {projectFile}.");
        }

        static void LogNoUserSecretsForProject(AbsolutePath profileFile)
        {
            Logger.Warn($"No user secrets have been added for project {profileFile}.");
        }

        static void LogNoUserSecretsProjects()
        {
            Logger.Warn("No user secrets projects have been added. Setting user secrets will be skipped.");
        }
    }
}
