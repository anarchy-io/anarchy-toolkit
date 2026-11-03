using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
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
        public IServiceCollection UseModule(IDependencyInjectionModule module,
                                            IConfiguration configuration,
                                            IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(module);

            return module.RegisterServices(services, configuration, environment);
        }
    }
}
