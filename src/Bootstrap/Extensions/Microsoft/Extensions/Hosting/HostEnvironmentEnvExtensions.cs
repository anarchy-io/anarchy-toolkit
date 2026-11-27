using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;

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
public static class HostEnvironmentEnvExtensions
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
    }
}
