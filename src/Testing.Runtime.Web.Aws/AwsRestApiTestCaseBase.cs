using Amazon.Lambda.TestUtilities;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Runtime.Web.Aws;
using Anarchy.Toolkit.Runtime.Web.Composition;
using Anarchy.Toolkit.Testing.Extensions.System.Net.Http;
using Anarchy.Toolkit.Testing.Helpers.Http;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Extensions.Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Extensions.System.Net.Http;
using Anarchy.Toolkit.Testing.Runtime.Web.Extensions.System.Net.Http;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws;

/// <summary>
/// Base class for end-to-end tests of REST APIs that are deployed both as
/// native ASP.NET Core applications (via <see cref="WebApplicationFactory{TStartup}"/>)
/// and as AWS Lambda functions (via <see cref="AwsWebAppEntryPointBase{T}"/>).
///
/// This abstraction allows writing a single test that can target:
/// <list type="bullet">
///   <item>
///     <description><b>Native execution</b>: calls the API through a test <see cref="HttpClient"/>.</description>
///   </item>
///   <item>
///     <description><b>Lambda execution</b>: simulates API Gateway + Lambda integration by invoking the
///     entrypoint with a constructed <c>APIGatewayProxyRequest</c>.</description>
///   </item>
/// </list>
///
/// Typical usage is to call <see cref="RestApiTestCaseBase{TStartup, TAppSettings}.TestEndpointNativeAsync{TParsedOutput}"/> and
/// <see cref="TestEndpointLambdaAsync{TParsedOutput}"/> separately,
/// or to use <see cref="TestEndpointAsync{TParsedOutput}"/> for deterministic endpoints where
/// responses should be identical across hosting models.
/// </summary>
/// <typeparam name="TStartup">The application startup type.</typeparam>
/// <typeparam name="TAppSettings">
/// The consumer's concrete application settings type. When it implements <see cref="IAuthServiceAppSettings"/>, its
/// test settings provide the mock token issuer; otherwise no token is added.
/// </typeparam>
/// <param name="awsWebAppEntryPoint">The AWS Lambda entrypoint used to simulate Lambda execution.</param>
/// <param name="factory">The web-application factory used to host the native pipeline.</param>
/// <param name="authorizationScopes">
/// The OAuth2 scope(s) embedded in the mock bearer token when
/// <c>includeAuthorizationToken</c> is <c>true</c>. Consumers supply their own scope strings.
/// </param>
/// <param name="controllerRouteBaseUrl">Optional base URL prepended to request routes.</param>
/// <remarks>
/// The Lambda path uses a <c>TestLambdaContext</c> and converts requests with
/// <see cref="ApiGatewayHttpRequestMessageExtensions"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public class OrdersApiTest(WebApplicationFactory&lt;Startup&gt; factory)
///     : AwsRestApiTestCaseBase&lt;Startup, AppSettings&gt;(
///         new TestableLambdaEntrypoint&lt;Startup&gt;(), factory, ["orders:read"], "api/orders")
/// {
///     [Fact]
///     public async Task GetOrder_ReturnsOk() =&gt;
///         await TestEndpointAsync&lt;OrderResponse&gt;(
///             Get("{id}", 42), HttpStatusCode.OK,
///             body =&gt; body!.Id.Should().Be(42));
/// }
/// </code>
/// </example>
public abstract class AwsRestApiTestCaseBase<TStartup, TAppSettings>(
    AwsWebAppEntryPointBase<TStartup> awsWebAppEntryPoint,
    WebApplicationFactory<TStartup> factory,
    string[] authorizationScopes,
    string? controllerRouteBaseUrl = null)
    : RestApiTestCaseBase<TStartup, TAppSettings>(factory, authorizationScopes, controllerRouteBaseUrl)
    where TStartup : class, IWebAppStartup
    where TAppSettings : class, IAppSettings
{
    /// <summary>
    /// For deterministic endpoints: executes the same request against both the native
    /// ASP.NET Core pipeline and the Lambda pipeline, asserts that their responses are
    /// equivalent, and then applies the provided body assertions.
    ///
    /// Use this when the endpoint is expected to return the <b>same value in both contexts</b>
    /// (e.g., static configuration, health status with stable fields).
    ///
    /// For endpoints that include variable fields (timestamps, durations, identifiers),
    /// prefer calling <see cref="RestApiTestCaseBase{TStartup, TAppSettings}.TestEndpointNativeAsync{TParsedOutput}"/> and
    /// <see cref="TestEndpointLambdaAsync{TParsedOutput}"/> separately, reusing a shared
    /// body assertion method.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected response model type.</typeparam>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="bodyAssertions">Optional domain-specific assertions on the deserialized body.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true); ignored when the settings have no auth service</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body of the Lambda execution (or <c>null</c> if empty).</returns>
    /// <remarks>
    /// The request body is read once and each pipeline receives its own clone. Assertions run inside an
    /// <see cref="AssertionScope"/>, so failures from both executions are reported together.
    /// </remarks>
    protected override async Task<TParsedOutput?> TestEndpointAsync<TParsedOutput>(
        HttpRequestMessage requestFixture,
        HttpStatusCode expectedStatusCode,
        Action<TParsedOutput?>? bodyAssertions = null,
        bool includeAuthorizationToken = true,
        CancellationToken cancellationToken = default) where TParsedOutput : class
    {
        using (new AssertionScope())
        {
            // Read request body for cloning the request to avoid issues with disposed content
            var requestBody = requestFixture.Content is not null ?
                await requestFixture.Content.ReadAsStringAsync(cancellationToken) :
                null;

            var responseNative = await TestEndpointNativeAsync(
                requestFixture.Clone(requestBody),
                expectedStatusCode,
                bodyAssertions,
                includeAuthorizationToken,
                cancellationToken);

            var responseLambda = await TestEndpointLambdaAsync(
                requestFixture.Clone(requestBody),
                expectedStatusCode,
                bodyAssertions,
                includeAuthorizationToken,
                cancellationToken);

            responseNative.Should().BeEquivalentTo(responseLambda);

            return responseLambda;
        }
    }

    /// <summary>
    /// Executes the request through the simulated AWS Lambda entrypoint,
    /// by translating it into an <c>APIGatewayProxyRequest</c> and invoking
    /// the entrypoint's function handler.
    /// Asserts the status code, then deserializes the body and applies the provided assertions.
    ///
    /// Use this when testing only the <b>Lambda execution path</b> or when responses are
    /// non-deterministic across execution models, and you want to validate them separately.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected response model type.</typeparam>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="bodyAssertions">Optional domain-specific assertions on the deserialized body.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true); ignored when the settings have no auth service</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body (or <c>null</c> if empty).</returns>
    protected async Task<TParsedOutput?> TestEndpointLambdaAsync<TParsedOutput>(
        HttpRequestMessage requestFixture,
        HttpStatusCode expectedStatusCode,
        Action<TParsedOutput?>? bodyAssertions = null,
        bool includeAuthorizationToken = true,
        CancellationToken cancellationToken = default) where TParsedOutput : class
    {
        if (includeAuthorizationToken) AddMockAuthorizationToken(requestFixture);

        var proxyRequest = await requestFixture.ToApiGatewayProxyRequestAsync(cancellationToken);

        var response = await awsWebAppEntryPoint.FunctionHandlerAsync(proxyRequest, new TestLambdaContext());

        response.Should().HaveStatusCodeOrDump((int)expectedStatusCode, allowRichOutput: false);

        var parsedBody = ResponseHelper.ParseJsonBody<TParsedOutput>(response.Body, "lambda");

        bodyAssertions?.Invoke(parsedBody);

        return parsedBody;
    }
}
