using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Assertion;
using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Extensions.Amazon.Lambda.APIGatewayEvents;

/// <summary>
/// Entry point extensions that enable AwesomeAssertions-style fluent assertions on
/// <see cref="APIGatewayProxyResponse"/> instances returned by Lambda handlers under test.
/// </summary>
/// <example>
/// <code language="csharp">
/// APIGatewayProxyResponse response = await handler.FunctionHandlerAsync(request, new LambdaContextFixture());
/// response.Should().HaveStatusCodeOrDump(200);
/// </code>
/// </example>
public static class ApiGatewayProxyResponseAssertionsExtensions
{
    /// <summary>
    /// Enables fluent assertions for <see cref="APIGatewayProxyResponse"/>.
    /// </summary>
    /// <param name="instance">The response under test.</param>
    /// <returns>An <see cref="ApiGatewayProxyResponseAssertions"/> bound to the current <see cref="AssertionChain"/>.</returns>
    public static ApiGatewayProxyResponseAssertions Should(this APIGatewayProxyResponse instance)
        => new(instance, AssertionChain.GetOrCreate());
}
