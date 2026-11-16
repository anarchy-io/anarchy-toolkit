using Anarchy.Toolkit.Configuration.Host;
using Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting;

/// <summary>
/// Extension members for <see cref="IHostBuilder"/> that bootstrap services through an <see cref="IStartup"/> class.
/// </summary>
public static class HostBuilderExtension
{
    /// <param name="builder">The host builder to configure.</param>
    extension(IHostBuilder builder)
    {
        /// <summary>
        /// Configures services through <typeparamref name="TStartup"/>: when services are configured, an instance is
        /// created with the host's <see cref="IHostEnvironment"/> and <see cref="IConfiguration"/>, an
        /// <see cref="AppRunner"/> for <paramref name="appRunnerType"/> is registered as a singleton, and
        /// <see cref="IStartup.ConfigureServices"/> is called.
        /// </summary>
        /// <typeparam name="TStartup">
        /// The startup type. Must have a public <c>(IHostEnvironment, IConfiguration)</c> constructor.
        /// </typeparam>
        /// <param name="appRunnerType">How the application is run.</param>
        /// <returns>The host builder, for chaining.</returns>
        /// <exception cref="MissingStartupConstructorException">
        /// Thrown if <typeparamref name="TStartup"/> has no public <c>(IHostEnvironment, IConfiguration)</c> constructor.
        /// </exception>
        public IHostBuilder UseStartup<TStartup>(AppRunner.Types appRunnerType)
            where TStartup : class, IStartup
        {
            return builder.UseStartup(typeof(TStartup), appRunnerType);
        }

        private IHostBuilder UseStartup(Type startupType, AppRunner.Types appRunnerType)
        {
            if (!typeof(IStartup).IsAssignableFrom(startupType))
            {
                throw new MissingStartupInterfaceException(startupType);
            }

            var ctor = startupType.GetConstructor([typeof(IHostEnvironment), typeof(IConfiguration)]);
            if (ctor is null)
            {
                throw new MissingStartupConstructorException(startupType);
            }

            return builder.ConfigureServices((context, services) => {
                var startup = (IStartup)ctor.Invoke([context.HostingEnvironment, context.Configuration]);

                services.AddSingleton(new AppRunner(appRunnerType));

                startup.ConfigureServices(services);
            });
        }
    }
}
