using Anarchy.Toolkit.Bootstrap.Host;
using Anarchy.Toolkit.Runtime.Extensions.Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Runtime.Host;

/// <summary>
/// Builds configured <see cref="IHost"/> instances for Jobs and Workers (EC2/systemd).
/// </summary>
/// <remarks>
/// Environment variables are always added as a configuration source. Any other source (e.g. the AWS SSM Parameter
/// Store through <c>AddAwsSystemsManagerParameterStore</c> from <c>Anarchy.Toolkit.Bootstrap.Aws</c>) is the application's choice, made through
/// <c>configureAppConfiguration</c>; it runs before the environment variables so those keep the last word.
/// <para>Both builders start from <see cref="Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()"/>, use
/// <see cref="AppContext.BaseDirectory"/> as content root and bootstrap services through the
/// <see cref="IStartup"/> type argument, registering an <see cref="AppRunner"/> describing the
/// runner type. Usually used through <see cref="JobEntryPointBase{TJobStartup}"/> or
/// <see cref="WorkerEntryPointBase{TJobStartup}"/>.</para>
/// </remarks>
public class AppHostBuilder
{
    /// <summary>
    /// Builds a Job (a local run or a serverless function such as AWS Lambda) for the given startup type.
    /// </summary>
    /// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
    /// <param name="configureAppConfiguration">Adds the application's own configuration sources.</param>
    /// <returns>The built, not yet started, host.</returns>
    /// <exception cref="Anarchy.Toolkit.Runtime.Host.Exceptions.MissingStartupConstructorException">
    /// Thrown if <typeparamref name="TJobStartup"/> has no <c>(IHostEnvironment, IConfiguration)</c> constructor.
    /// </exception>
    public static IHost ForJob<TJobStartup>(
        Action<HostBuilderContext, IConfigurationBuilder>? configureAppConfiguration = null)
        where TJobStartup : class, IStartup
    {
        return BaseBuilder<TJobStartup>(AppRunner.Types.Job, configureAppConfiguration)
                .Build()
            ;
    }

    /// <summary>
    /// Builds an EC2/systemd-hosted Worker for the given startup type.
    /// </summary>
    /// <remarks>
    /// Additionally enables systemd integration (<c>UseSystemd</c>) and reads <c>ASPNETCORE_</c>-prefixed environment
    /// variables into the host configuration.
    /// </remarks>
    /// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
    /// <param name="configureAppConfiguration">Adds the application's own configuration sources.</param>
    /// <returns>The built, not yet started, host.</returns>
    /// <exception cref="Anarchy.Toolkit.Runtime.Host.Exceptions.MissingStartupConstructorException">
    /// Thrown if <typeparamref name="TJobStartup"/> has no <c>(IHostEnvironment, IConfiguration)</c> constructor.
    /// </exception>
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
