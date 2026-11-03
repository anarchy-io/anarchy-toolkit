using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Helper.Assertions.FailureOutput;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Anarchy.Toolkit.Testing.Assertion;

public class ApiGatewayProxyResponseAssertions(APIGatewayProxyResponse subject, AssertionChain assertionChain)
    : ReferenceTypeAssertions<APIGatewayProxyResponse, ApiGatewayProxyResponseAssertions>(subject, assertionChain)
{
    private readonly AssertionChain _assertionChain = assertionChain;

    protected override string Identifier => nameof(APIGatewayProxyResponse);

    public AndConstraint<ApiGatewayProxyResponseAssertions> BeSuccessfulOrDump(
        bool allowRichOutput = true, string because = "", params object[] becauseArgs)
    {
        return AssertCondition(
            allowRichOutput,
            Subject.StatusCode is >= 200 and < 400,
            $"Expected {{context:APIGatewayProxyResponse}} to have a 2xx or 3xx status code but found {Subject.StatusCode}.",
            because,
            becauseArgs
        );
    }

    public AndConstraint<ApiGatewayProxyResponseAssertions> HaveStatusCodeOrDump(
        int expectedStatusCode, bool allowRichOutput = true, string because = "", params object[] becauseArgs)
    {
        return AssertCondition(
            allowRichOutput,
            Subject.StatusCode == expectedStatusCode,
            $"Expected {{context:APIGatewayProxyResponse}} to have status code {expectedStatusCode} but found {Subject.StatusCode}.",
            because,
            becauseArgs
        );
    }

    private AndConstraint<ApiGatewayProxyResponseAssertions> AssertCondition(
        bool allowRichOutput,
        bool condition,
        string failMessage,
        string because,
        object[] becauseArgs)
    {
        _assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(condition)
            .FailWith("{0}\n\n{1}", failMessage, ApiGatewayResponseFailureOutput.Format(Subject, allowRichOutput));

        return new AndConstraint<ApiGatewayProxyResponseAssertions>(this);
    }
}
