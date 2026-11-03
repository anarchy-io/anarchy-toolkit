using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Assertion;
using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Testing.Extension.Amazon.Lambda.APIGatewayEvents;

public static class ApiGatewayProxyResponseAssertionsExtensions
{
    /// <summary>
    /// Enables fluent assertions for <see cref="APIGatewayProxyResponse"/>.
    /// </summary>
    public static ApiGatewayProxyResponseAssertions Should(this APIGatewayProxyResponse instance)
        => new(instance, AssertionChain.GetOrCreate());
}
