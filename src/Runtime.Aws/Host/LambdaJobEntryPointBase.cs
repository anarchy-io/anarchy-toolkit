using Anarchy.Toolkit.Bootstrap.Aws.Telemetry;
using Anarchy.Toolkit.Runtime.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Runtime.Aws.Host;

/// <summary>
/// Base entry point for Jobs and event handlers hosted as AWS Lambda functions.
/// </summary>
/// <remarks>
/// The package references <c>Amazon.Lambda.Core</c>, so handlers can take an <c>ILambdaContext</c>.
/// The host is built once per execution environment, as in <see cref="JobEntryPointBase{TJobStartup}"/>. Each handler
/// starts its invocation with <see cref="BeginInvocation"/>, which stores the invocation's X-Ray trace id in
/// <see cref="AmazonRequestContext"/> and opens a service scope.
/// </remarks>
/// <typeparam name="TJobStartup">The startup type used to configure services.</typeparam>
/// <example>
/// <code language="csharp">
/// public sealed class EntryPointLambda : LambdaJobEntryPointBase&lt;Startup&gt;
/// {
///     public async Task HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
///     {
///         await using var scope = BeginInvocation();
///         await scope.ServiceProvider.GetRequiredService&lt;IOrderHandler&gt;().HandleAsync(sqsEvent);
///     }
/// }
/// </code>
/// </example>
public abstract class LambdaJobEntryPointBase<TJobStartup> : JobEntryPointBase<TJobStartup>
    where TJobStartup : class, IStartup
{
    /// <summary>
    /// The environment variable in which the Lambda runtime exposes the X-Ray trace id of the current invocation.
    /// </summary>
    public const string TraceIdEnvironmentVariable = "_X_AMZN_TRACE_ID";

    /// <summary>
    /// Starts an invocation: stores its X-Ray trace id (from <see cref="TraceIdEnvironmentVariable"/>) in
    /// <see cref="AmazonRequestContext"/> for the current async flow and creates a service scope for it.
    /// </summary>
    /// <returns>The invocation's service scope; dispose it when the invocation ends.</returns>
    protected AsyncServiceScope BeginInvocation()
    {
        AmazonRequestContext.SetValues(Environment.GetEnvironmentVariable(TraceIdEnvironmentVariable));
        return App.Services.CreateAsyncScope();
    }
}
