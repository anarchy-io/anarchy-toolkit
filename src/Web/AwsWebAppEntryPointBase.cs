using Amazon.Lambda.AspNetCoreServer;
using Anarchy.Toolkit.Hosting.Host.Exceptions;
using Anarchy.Toolkit.Web.Composition;

namespace Anarchy.Toolkit.Web;

/// <summary>
///     This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the
///     actual Lambda function entry point. The Lambda handler field should be set to
///     &lt;Assembly&gt;::&lt;Namespace&gt;.WebEntryPoint::FunctionHandlerAsync
/// </summary>
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
    /// <param name="builder"></param>
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
    /// <param name="builder"></param>
    protected override void Init(IHostBuilder builder)
    {
    }

    /// <summary>
    /// An overridable method that determines the project's name
    /// </summary>
    /// <returns>string</returns>
    protected virtual string GetAppName()
    {
        return GetType().Assembly.GetName().Name?.Split(".").Last() ??
               throw new AppNameNotResolvedException(GetType());
    }

    /// <summary>
    /// Configures application settings for the REST API host by loading AWS Systems Manager
    /// Parameter Store parameters for the current application and environment, followed by
    /// host environment variables. This allows running the Lambda without physical configuration
    /// files while still supporting environment overrides and cloud-managed parameters.
    /// </summary>
    /// <summary>
    /// Adds the application's own configuration sources (e.g. the AWS SSM Parameter Store through
    /// <c>AddAwsSystemsManagerParameterStore</c>) before the environment variables.
    /// </summary>
    protected virtual void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
    {
    }
}
