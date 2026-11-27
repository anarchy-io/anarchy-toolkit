using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Core.Composition.Module;

/// <summary>
/// A self-contained unit of dependency injection registrations (a feature, an adapter, an integration...).
/// </summary>
/// <remarks>
/// Modules are applied with the <c>UseModule</c> extensions in
/// <see cref="ServiceCollectionExtensions"/>, which pass in the application
/// configuration and hosting environment so the module can bind options or vary registrations per environment.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class MyFeatureModule : IDependencyInjectionModule
/// {
///     public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
///                                                IHostEnvironment environment)
///     {
///         services.AddSingleton&lt;IMyService, MyService&gt;();
///         return services;
///     }
/// }
/// </code>
/// </example>
public interface IDependencyInjectionModule
{
    /// <summary>
    /// Registers the module's services.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection (usually <paramref name="services"/>), for chaining.</returns>
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                        IHostEnvironment environment);
}
