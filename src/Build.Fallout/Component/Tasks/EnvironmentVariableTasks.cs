using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Logger = Anarchy.Toolkit.Build.Cli.Infrastructure.Helper.Logger;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    /// <summary>
    /// Tasks related to setting up environment variables.
    /// </summary>
    public static class EnvironmentVariableTasks
    {
        /// <summary>
        /// Sets environment values from the specified configuration dictionary, after optionally decrypting the values.
        /// </summary>
        public static void SetEnvironmentVariables(this IDefaultBuild build, IReadOnlyDictionary<string, ConfigurationValue> values)
        {
            foreach ((string key, var value) in values)
            {
                string plainTextValue = value switch {
                    Secret secret => build.DecryptSecret(secret),
                    _ => value.Value,
                };
                System.Environment.SetEnvironmentVariable(key, plainTextValue);
            }

            Logger.Info("Set the following environment variables: {0}", string.Join(", ", values.Keys));
        }
    }
}
