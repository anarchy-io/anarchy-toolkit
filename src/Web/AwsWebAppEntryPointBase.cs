using Amazon.Lambda.AspNetCoreServer;
using Anarchy.Toolkit.Hosting.Host.Exceptions;
using Anarchy.Toolkit.Web.Composition;

namespace Anarchy.Toolkit.Web;

/// <summary>
///     This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the
///     actual Lambda function entry point. The Lambda handler field should be set to
///     &lt;Assembly&gt;::&lt;Namespace&gt;.WebEntryPoint::FunctionHandlerAsync
/// </summary>
/// <remarks>
/// <see cref="Init(IWebHostBuilder)"/> adds the sources from <see cref="ConfigureAppConfiguration"/> followed by
/// environment variables and uses <typeparamref name="T"/> as startup. The base class targets API Gateway REST APIs
/// and HTTP API payload version 1.0.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class WebEntryPoint : AwsWebAppEntryPointBase&lt;Startup&gt;
/// {
///     protected override void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
///     {
///         // e.g. add the AWS SSM Parameter Store for this application
///     }
/// }
/// </code>
/// </example>
/// <typeparam name="T">The startup class of the web application.</typeparam>
public abstract class AwsWebAppEntryPointBase<T> :
    // The base class must be set to match the AWS service invoking the Lambda function. If not Amazon.Lambda.AspNetCoreServer
    // will fail to convert the incoming request correctly into a valid ASP.NET Core request.
    //
    // API Gateway REST API                         -> Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    // API Gateway HTTP API payload version 1.0     -> Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    // API Gateway HTTP API payload version 2.0     -> Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction
    // Application Load Balancer                    -> Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
    //
    // Note: When using the AWS::Serverless::Function resource with an event type of "HttpApi" then payload version 2.0
    // will be the default, and you must make Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction the base class.
    APIGatewayProxyFunction where T : class, IWebAppStartup
{
    /// <summary>
    ///     The builder has configuration, logging and Amazon API Gateway already configured. The startup class
    ///     needs to be configured in this method using the UseStartup&lt;&gt;() method.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration((context, configuration) => {
                ConfigureAppConfiguration(context, configuration);
                configuration.AddEnvironmentVariables();
            })
            .UseStartup<T>();
    }

    /// <summary>
    ///     Use this override to customize the services registered with the IHostBuilder.
    ///     It is recommended not to call ConfigureWebHostDefaults to configure the IWebHostBuilder inside this method.
    ///     Instead, customize the IWebHostBuilder in the Init(IWebHostBuilder) overload.
    /// </summary>
    /// <param name="builder">The generic host builder. The base implementation does nothing.</param>
    protected override void Init(IHostBuilder builder)
    {
    }

    /// <summary>
    /// An overridable method that determines the project's name
    /// </summary>
    /// <remarks>Defaults to the last dot-separated segment of the entry point's assembly name.</remarks>
    /// <returns>The application name.</returns>
    /// <exception cref="Anarchy.Toolkit.Hosting.Host.Exceptions.AppNameNotResolvedException">The assembly name cannot be resolved.</exception>
    protected virtual string GetAppName()
    {
        return GetType().Assembly.GetName().Name?.Split(".").Last() ??
               throw new AppNameNotResolvedException(GetType());
    }

    /// <summary>
    /// Adds the application's own configuration sources (e.g. the AWS SSM Parameter Store through
    /// <c>AddAwsSystemsManagerParameterStore</c>) before the environment variables.
    /// </summary>
    /// <remarks>
    /// This allows running the Lambda without physical configuration files while still supporting environment
    /// overrides and cloud-managed parameters. The base implementation adds nothing.
    /// </remarks>
    /// <param name="context">The web host builder context.</param>
    /// <param name="builder">The configuration builder to add sources to.</param>
    protected virtual void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
    {
    }
}
