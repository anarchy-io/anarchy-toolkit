using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions to register <see cref="IDependencyInjectionModule"/>s.
/// </summary>
/// <example>
/// <code language="csharp">
/// var builder = Host.CreateApplicationBuilder(args);
///
/// builder.Services
///     .UseModule&lt;MyFeatureModule&gt;(builder.Configuration, builder.Environment)
///     .UseModule(new MyOptionsModule("MySection"), builder.Configuration, builder.Environment);
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection to register into.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Instantiates <typeparamref name="T"/> through its public parameterless constructor and lets it register its
        /// services.
        /// </summary>
        /// <typeparam name="T">The module type; must have a public parameterless constructor.</typeparam>
        /// <param name="configuration">The application configuration passed to the module.</param>
        /// <param name="environment">The hosting environment passed to the module.</param>
        /// <returns>The collection returned by the module, for chaining.</returns>
        public IServiceCollection UseModule<T>(IConfiguration configuration,
                                               IHostEnvironment environment)
            where T : class, IDependencyInjectionModule
        {
            return services.UseModule(Activator.CreateInstance<T>(), configuration, environment);
        }

        /// <summary>
        /// Registers an already-built module, for modules that take constructor parameters
        /// (e.g. the configuration section to bind their options from).
        /// </summary>
        /// <param name="module">The module instance.</param>
        /// <param name="configuration">The application configuration passed to the module.</param>
        /// <param name="environment">The hosting environment passed to the module.</param>
        /// <returns>The collection returned by <see cref="IDependencyInjectionModule.RegisterServices"/>, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="module"/> is <see langword="null"/>.</exception>
        public IServiceCollection UseModule(IDependencyInjectionModule module,
                                            IConfiguration configuration,
                                            IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(module);

            return module.RegisterServices(services, configuration, environment);
        }
    }
}
