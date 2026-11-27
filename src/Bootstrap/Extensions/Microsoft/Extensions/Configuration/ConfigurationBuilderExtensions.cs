using Anarchy.Toolkit.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Configuration;

/// <summary>
/// Extension methods for <see cref="IConfigurationBuilder"/> that add the toolkit's configuration sources.
/// </summary>
public static class ConfigurationBuilderExtensions
{
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
