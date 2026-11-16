using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Service.AppSecret;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;

/// <summary>
/// Extension members for <see cref="IHostEnvironment"/> that understand the toolkit's environments
/// (<see cref="AppEnvironments"/>) and distinguish local from remote (deployed) execution.
/// </summary>
/// <example>
/// <code language="csharp">
/// if (environment.IsLocal())
/// {
///     // use local fakes
/// }
/// </code>
/// </example>
public static class HostEnvironmentEnvExtension
{
    /// <param name="hostEnvironment">The host environment to inspect.</param>
    extension(IHostEnvironment hostEnvironment)
    {
        /// <summary>
        /// Determines whether the application runs locally: in <see cref="AppEnvironments.Development"/> or in any
        /// environment that is not remote (see <see cref="IsRemote"/>).
        /// </summary>
        /// <returns><see langword="true"/> when running locally.</returns>
        public bool IsLocal()
        {
            return hostEnvironment.IsDevelopment() || !hostEnvironment.IsRemote();
        }

        /// <summary>
        /// Determines whether the application runs in a deployed environment: <see cref="AppEnvironments.Testing"/>,
        /// <see cref="AppEnvironments.Acceptance"/>, <see cref="AppEnvironments.Staging"/> or
        /// <see cref="AppEnvironments.Production"/>.
        /// </summary>
        /// <returns><see langword="true"/> when running in a remote environment.</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool IsRemote()
        {
            return hostEnvironment.IsTesting() || hostEnvironment.IsAcceptance() || hostEnvironment.IsStaging() || hostEnvironment.IsProduction();
        }

        /// <summary>
        /// Determines whether the environment is <see cref="AppEnvironments.Testing"/>.
        /// </summary>
        /// <returns><see langword="true"/> for the Testing environment.</returns>
        public bool IsTesting()
        {
            return hostEnvironment.IsEnvironment(AppEnvironments.Testing);
        }

        /// <summary>
        /// Determines whether the environment is <see cref="AppEnvironments.Acceptance"/>.
        /// </summary>
        /// <returns><see langword="true"/> for the Acceptance environment.</returns>
        public bool IsAcceptance()
        {
            return hostEnvironment.IsEnvironment(AppEnvironments.Acceptance);
        }

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
        /// <exception cref="Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions.SecretNotFoundException">
        /// Thrown remotely when the secret has no string value.
        /// </exception>
        /// <exception cref="Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions.SecretRetrievalException">
        /// Thrown remotely when the secret cannot be retrieved.
        /// </exception>
        public string GetSecretValue(string secretName)
        {
            return hostEnvironment.IsLocal() ? secretName : SecretManagerManagementService.GetSecretWithCache(secretName);
        }
    }
}
