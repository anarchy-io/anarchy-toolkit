using Anarchy.Toolkit.Bootstrap.Aws.Service.AppSecret;
using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Bootstrap.Aws.Extensions.Microsoft.Extensions.Hosting;

/// <summary>
/// AWS extension members for <see cref="IHostEnvironment"/>.
/// </summary>
public static class AwsHostEnvironmentExtensions
{
    /// <param name="hostEnvironment">The host environment to inspect.</param>
    extension(IHostEnvironment hostEnvironment)
    {
        /// <summary>
        /// Resolves a secret value from AWS Secrets Manager (cached per process) when running remotely.
        /// </summary>
        /// <remarks>
        /// Locally, <paramref name="secretName"/> itself is returned, so local configuration can hold the plain value
        /// where remote configuration holds the secret name. Remotely this call blocks synchronously; see
        /// <see cref="SecretManagerManagementService.GetSecretWithCache"/>.
        /// </remarks>
        /// <param name="secretName">The secret name (remote) or the plain value (local).</param>
        /// <returns>The secret value.</returns>
        /// <exception cref="Anarchy.Toolkit.Bootstrap.Service.AppSecret.Exceptions.SecretNotFoundException">
        /// Thrown remotely when the secret has no string value.
        /// </exception>
        /// <exception cref="Anarchy.Toolkit.Bootstrap.Service.AppSecret.Exceptions.SecretRetrievalException">
        /// Thrown remotely when the secret cannot be retrieved.
        /// </exception>
        public string GetAwsSecretValue(string secretName)
        {
            return hostEnvironment.IsLocal() ? secretName : SecretManagerManagementService.GetSecretWithCache(secretName);
        }
    }
}
