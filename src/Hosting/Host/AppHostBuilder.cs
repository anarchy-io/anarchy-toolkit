using Anarchy.Toolkit.Configuration.Host;
using Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Host;

/// <summary>
/// Builds configured <see cref="IHost"/> instances for Jobs (Lambda) and Workers (EC2/systemd).
/// </summary>
/// <remarks>
/// Environment variables are always added as a configuration source. Any other source (e.g. the AWS SSM Parameter
/// Store through <c>AddAwsSystemsManagerParameterStore</c>) is the application's choice, made through
/// <c>configureAppConfiguration</c>; it runs before the environment variables so those keep the last word.
/// </remarks>
public class AppHostBuilder
{
    /// <summary>
    /// Builds a Lambda-hosted Job for the given startup type.
    /// </summary>
    /// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
    /// <param name="configureAppConfiguration">Adds the application's own configuration sources.</param>
    public static IHost ForJob<TJobStartup>(
        Action<HostBuilderContext, IConfigurationBuilder>? configureAppConfiguration = null)
        where TJobStartup : class, IStartup
    {
        return BaseBuilder<TJobStartup>(AppRunner.Types.Lambda, configureAppConfiguration)
                .Build()
            ;
    }

    /// <summary>
    /// Builds an EC2/systemd-hosted Worker for the given startup type.
    /// </summary>
    /// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
    /// <param name="configureAppConfiguration">Adds the application's own configuration sources.</param>
    public static IHost ForWorker<TJobStartup>(
        Action<HostBuilderContext, IConfigurationBuilder>? configureAppConfiguration = null)
        where TJobStartup : class, IStartup
    {
        return BaseBuilder<TJobStartup>(AppRunner.Types.Ec2Worker, configureAppConfiguration)
                .UseSystemd()
                .ConfigureHostConfiguration(builderConfig => builderConfig.AddEnvironmentVariables("ASPNETCORE_"))
                .Build()
            ;
    }

    private static IHostBuilder BaseBuilder<TJobStartup>(
        AppRunner.Types appRunnerType,
        Action<HostBuilderContext, IConfigurationBuilder>? configureAppConfiguration)
        where TJobStartup : class, IStartup
    {
        return Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureAppConfiguration((context, builderConfig) => {
                    configureAppConfiguration?.Invoke(context, builderConfig);
                    builderConfig.AddEnvironmentVariables();
                })
                .UseStartup<TJobStartup>(appRunnerType)
            ;
    }
}
