using Anarchy.Toolkit.Configuration.Host;
using Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting;

public static class HostBuilderExtension
{
    extension(IHostBuilder builder)
    {
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
