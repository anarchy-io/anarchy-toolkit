using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Extensions.Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Helpers.Assertions.FailureOutput;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Assertion;

/// <summary>
/// AwesomeAssertions assertions for <see cref="APIGatewayProxyResponse"/>. On failure, the full response
/// (status code, headers and pretty-printed body) is dumped into the failure message to ease debugging.
/// </summary>
/// <remarks>
/// Obtain an instance via
/// <see cref="ApiGatewayProxyResponseAssertionsExtensions.Should"/>.
/// The dump is formatted by <see cref="ApiGatewayResponseFailureOutput.Format(APIGatewayProxyResponse, bool)"/>.
/// </remarks>
/// <param name="subject">The response under test.</param>
/// <param name="assertionChain">The assertion chain used to report failures.</param>
public class ApiGatewayProxyResponseAssertions(APIGatewayProxyResponse subject, AssertionChain assertionChain)
    : ReferenceTypeAssertions<APIGatewayProxyResponse, ApiGatewayProxyResponseAssertions>(subject, assertionChain)
{
    private readonly AssertionChain _assertionChain = assertionChain;

    /// <inheritdoc/>
    protected override string Identifier => nameof(APIGatewayProxyResponse);

    /// <summary>
    /// Asserts the response has a 2xx or 3xx status code, dumping the response on failure.
    /// </summary>
    /// <param name="allowRichOutput">
    /// Whether a Spectre.Console table may be used for the dump when the console supports ANSI and is interactive;
    /// otherwise plain text is used.
    /// </param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format into <paramref name="because"/>.</param>
    /// <returns>An <see cref="AndConstraint{T}"/> for chaining further assertions.</returns>
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

    /// <summary>
    /// Asserts the response has exactly <paramref name="expectedStatusCode"/>, dumping the response on failure.
    /// </summary>
    /// <param name="expectedStatusCode">The expected HTTP status code.</param>
    /// <param name="allowRichOutput">
    /// Whether a Spectre.Console table may be used for the dump when the console supports ANSI and is interactive;
    /// otherwise plain text is used.
    /// </param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format into <paramref name="because"/>.</param>
    /// <returns>An <see cref="AndConstraint{T}"/> for chaining further assertions.</returns>
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
