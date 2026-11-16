using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;

/// <summary>
/// Extension methods for <see cref="IConfigurationBuilder"/> that add the toolkit's configuration sources.
/// </summary>
public static class ConfigurationBuilderExtension
{
    /// <summary>
    /// Adds the AWS Systems Manager (SSM) Parameter Store as a configuration source when running in a TAP environment.
    /// </summary>
    /// <remarks>
    /// <para>Does nothing when the environment is local (see
    /// <see cref="Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting.HostEnvironmentEnvExtension"/>).</para>
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

    /// <summary>
    /// Adds one or more embedded JSON files from the specified assembly to the configuration builder.
    /// Files are loaded in the order provided, and environment-specific overrides can be included.
    /// </summary>
    /// <remarks>
    /// Resources that cannot be found or read are skipped with a message written to the console.
    /// </remarks>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to augment.</param>
    /// <param name="assembly">The assembly containing the embedded JSON resources.</param>
    /// <param name="resourceNames">The full names of the embedded JSON resources to load, in load order.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="assembly"/> is <see langword="null"/>.</exception>
    /// <exception cref="EmptyArgumentException">
    /// Thrown if <paramref name="resourceNames"/> is <see langword="null"/> or empty, or contains a
    /// <see langword="null"/>, empty or whitespace name.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    /// builder.AddEmbeddedJson(typeof(Program).Assembly,
    ///     "MyApp.appsettings.json",
    ///     $"MyApp.appsettings.{environment.EnvironmentName}.json");
    /// </code>
    /// </example>
    public static IConfigurationBuilder AddEmbeddedJson(
        this IConfigurationBuilder builder,
        Assembly assembly,
        params string[] resourceNames)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (resourceNames is null || resourceNames.Length == 0)
            throw new EmptyArgumentException(nameof(resourceNames), "At least one embedded resource name must be provided.");

        foreach (var resourceName in resourceNames)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new EmptyArgumentException(nameof(resourceNames), "Embedded resource name cannot be null or whitespace.");

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is { CanRead: true })
                builder.AddJsonStream(stream);
            else
                Console.WriteLine("Embedded settings file '{0}' could not be read.", resourceName);
        }

        return builder;
    }
}
