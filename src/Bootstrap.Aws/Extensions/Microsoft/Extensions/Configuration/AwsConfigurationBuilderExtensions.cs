using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Anarchy.Toolkit.Bootstrap.Aws.Extensions.Microsoft.Extensions.Configuration;

/// <summary>
/// AWS extension methods for <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class AwsConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds the AWS Systems Manager (SSM) Parameter Store as a configuration source when running in a remote environment.
    /// </summary>
    /// <remarks>
    /// <para>Does nothing when the environment is local (see
    /// <see cref="Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting.HostEnvironmentEnvExtensions.IsLocal"/>).</para>
    /// <para>The SSM path is read from the <c>PARAMETER_STORE_CONFIGURATION_PATH</c> environment variable. When it is
    /// not set, it is derived as <c>{parameterStorePathPrefix}/{workload}</c>, where <c>workload</c> is the entry
    /// assembly name (or <see cref="IHostEnvironment.ApplicationName"/>) without <paramref name="appNamePrefix"/> and
    /// with dots replaced by slashes; the derived value is then written back to that environment variable.</para>
    /// </remarks>
    /// <param name="builder">The configuration builder to augment.</param>
    /// <param name="environment">The host environment used to determine whether SSM should be enabled.</param>
    /// <param name="parameterStorePathPrefix">
    /// The root SSM path prefix (e.g. <c>/my-domain/</c>). The trailing workload segment is derived
    /// from the application/assembly name and appended to this prefix.
    /// </param>
    /// <param name="appNamePrefix">
    /// An optional application-name prefix (for example <c>MyCompany.</c>) that is stripped from the derived
    /// workload name before building the SSM path. When <c>null</c> or empty, no prefix is stripped.
    /// </param>
    /// <returns>The <see cref="IConfigurationBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="EmptyArgumentException">
    /// Thrown if <paramref name="parameterStorePathPrefix"/> is <see langword="null"/>, empty or whitespace.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    /// // Entry assembly "MyCompany.Orders.Worker" → SSM path "/my-domain/Orders/Worker"
    /// builder.Configuration.AddAwsSystemsManagerParameterStore(builder.Environment, "/my-domain/", "MyCompany.");
    /// </code>
    /// </example>
    public static IConfigurationBuilder AddAwsSystemsManagerParameterStore(
        this IConfigurationBuilder builder,
        IHostEnvironment environment,
        string parameterStorePathPrefix,
        string? appNamePrefix = null)
    {
        if (string.IsNullOrWhiteSpace(parameterStorePathPrefix))
            throw new EmptyArgumentException(nameof(parameterStorePathPrefix), "Parameter Store path prefix cannot be empty.");

        if (environment.IsLocal())
        {
            return builder;
        }

        var parameterStoreConfigurationPath = Environment.GetEnvironmentVariable("PARAMETER_STORE_CONFIGURATION_PATH");

        if (string.IsNullOrWhiteSpace(parameterStoreConfigurationPath))
        {
            parameterStoreConfigurationPath =
                DeriveParameterStoreConfigurationPath(environment, parameterStorePathPrefix, appNamePrefix);
            Environment.SetEnvironmentVariable("PARAMETER_STORE_CONFIGURATION_PATH", parameterStoreConfigurationPath);
        }

        builder.AddSystemsManager(parameterStoreConfigurationPath);

        return builder;
    }

    private static string DeriveParameterStoreConfigurationPath(
        IHostEnvironment environment,
        string parameterStorePathPrefix,
        string? appNamePrefix)
    {
        var applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? environment.ApplicationName;

        var workloadName = !string.IsNullOrEmpty(appNamePrefix)
                           && applicationName.StartsWith(appNamePrefix, StringComparison.Ordinal)
            ? applicationName[appNamePrefix.Length..]
            : applicationName;

        var prefix = parameterStorePathPrefix.EndsWith('/')
            ? parameterStorePathPrefix
            : parameterStorePathPrefix + "/";

        return $"{prefix}{workloadName.Replace('.', '/')}";
    }
}
