using Amazon.Lambda.TestUtilities;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Testing.Extension.Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Extension.System.Net.Http;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Testing.Web.Extension.System.Net.Http;
using Anarchy.Toolkit.Web;
using Anarchy.Toolkit.Web.Composition;
using Anarchy.Toolkit.Web.Helper.Presentation.Contract;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Anarchy.Toolkit.Testing.Web;

/// <summary>
/// Base class for acceptance tests of REST APIs that are deployed both as
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
/// Typical usage is to call <see cref="TestEndpointNativeAsync{TParsedOutput}"/> and
/// <see cref="TestEndpointLambdaAsync{TParsedOutput}"/> separately,
/// or to use <see cref="TestEndpointAsync{TParsedOutput}"/> for deterministic endpoints where
/// responses should be identical across hosting models.
/// </summary>
/// <typeparam name="TStartup">The application startup type.</typeparam>
/// <typeparam name="TAppSettings">
/// The consumer's concrete application settings type, whose test settings provide the mock token issuer.
/// </typeparam>
/// <param name="awsWebAppEntryPoint">The AWS Lambda entrypoint used to simulate Lambda execution.</param>
/// <param name="factory">The web-application factory used to host the native pipeline.</param>
/// <param name="authorizationScopes">
/// The OAuth2 scope(s) embedded in the mock bearer token when
/// <c>includeAuthorizationToken</c> is <c>true</c>. Consumers supply their own scope strings.
/// </param>
/// <param name="controllerRouteBaseUrl">Optional base URL prepended to request routes.</param>
public abstract class RestApiTestCaseBase<TStartup, TAppSettings>(
    AwsWebAppEntryPointBase<TStartup> awsWebAppEntryPoint,
    WebApplicationFactory<TStartup> factory,
    string[] authorizationScopes,
    string? controllerRouteBaseUrl = null)
    : IClassFixture<WebApplicationFactory<TStartup>>
    where TStartup : class, IWebAppStartup
    where TAppSettings : class, IAuthServiceAppSettings
{
    protected readonly HttpClient Client = factory.CreateClient();

    protected HttpRequestMessage Post(string route, params object[] routeParameters) =>
        Request(HttpMethod.Post, route, routeParameters);

    protected HttpRequestMessage Get(string route, params object[] routeParameters) =>
        Request(HttpMethod.Get, route, routeParameters);

    protected HttpRequestMessage Put(string route, params object[] routeParameters) =>
        Request(HttpMethod.Put, route, routeParameters);

    protected HttpRequestMessage Patch(string route, params object[] routeParameters) =>
        Request(HttpMethod.Patch, route, routeParameters);

    protected HttpRequestMessage Request(HttpMethod method, string route, params object[] routeParameters) =>
        new(method, HttpRouteHelper.FromTemplate(route, controllerRouteBaseUrl, routeParameters));

    /// <summary>
    /// For endpoints with no response body: executes the same request against both the native
    /// ASP.NET Core pipeline and the Lambda pipeline, and asserts that their status codes match.
    ///
    /// Use this when the endpoint is expected to return the <b>same response in both contexts</b>
    /// (e.g., static configuration, health status with stable fields).
    ///
    /// For endpoints that include variable fields (timestamps, durations, identifiers),
    /// prefer calling <see cref="TestEndpointNativeAsync{TParsedOutput}"/> and
    /// <see cref="TestEndpointLambdaAsync{TParsedOutput}"/> separately, reusing a shared
    /// body assertion method.
    ///
    /// </summary>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true)</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>void</returns>
    protected async Task TestEndpointAsync(HttpRequestMessage requestFixture, HttpStatusCode expectedStatusCode,
                                           bool includeAuthorizationToken = true,
                                           CancellationToken cancellationToken = default)
    {
        await TestEndpointAsync<object>(requestFixture, expectedStatusCode,
            includeAuthorizationToken: includeAuthorizationToken,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// For deterministic endpoints: executes the same request against both the native
    /// ASP.NET Core pipeline and the Lambda pipeline, asserts that their responses are
    /// equivalent, and then applies the provided body assertions.
    ///
    /// Use this when the endpoint is expected to return the <b>same value in both contexts</b>
    /// (e.g., static configuration, health status with stable fields).
    ///
    /// For endpoints that include variable fields (timestamps, durations, identifiers),
    /// prefer calling <see cref="TestEndpointNativeAsync{TParsedOutput}"/> and
    /// <see cref="TestEndpointLambdaAsync{TParsedOutput}"/> separately, reusing a shared
    /// body assertion method.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected response model type.</typeparam>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="bodyAssertions">Optional domain-specific assertions on the deserialized body.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true)</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body (or <c>null</c> if empty).</returns>
    protected async Task<TParsedOutput?> TestEndpointAsync<TParsedOutput>(
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
    /// Executes the request against the <see cref="WebApplicationFactory{TStartup}"/>-hosted API
    /// and asserts the status code, then deserializes the body and applies the provided assertions.
    ///
    /// Use this when testing only the <b>native execution path</b> or when responses are
    /// non-deterministic across execution models, and you want to validate them separately.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected response model type.</typeparam>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="bodyAssertions">Optional domain-specific assertions on the deserialized body.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true)</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body (or <c>null</c> if empty).</returns>
    protected async Task<TParsedOutput?> TestEndpointNativeAsync<TParsedOutput>(
        HttpRequestMessage requestFixture,
        HttpStatusCode expectedStatusCode,
        Action<TParsedOutput?>? bodyAssertions = null,
        bool includeAuthorizationToken = true,
        CancellationToken cancellationToken = default) where TParsedOutput : class
    {
        if (includeAuthorizationToken) requestFixture.MockAuthorizationToken<TAppSettings>(authorizationScopes);

        var response = await Client.SendAsync(requestFixture, cancellationToken);
        response.StatusCode.Should().Be(expectedStatusCode);

        var parsedBody = await ResponseHelper.ParseHttpResponse<TParsedOutput>(response, cancellationToken);

        bodyAssertions?.Invoke(parsedBody);

        return parsedBody;
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
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true)</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body (or <c>null</c> if empty).</returns>
    protected async Task<TParsedOutput?> TestEndpointLambdaAsync<TParsedOutput>(
        HttpRequestMessage requestFixture,
        HttpStatusCode expectedStatusCode,
        Action<TParsedOutput?>? bodyAssertions = null,
        bool includeAuthorizationToken = true,
        CancellationToken cancellationToken = default) where TParsedOutput : class
    {
        if (includeAuthorizationToken) requestFixture.MockAuthorizationToken<TAppSettings>(authorizationScopes);

        var proxyRequest = await requestFixture.ToApiGatewayProxyRequestAsync(cancellationToken);

        var response = await awsWebAppEntryPoint.FunctionHandlerAsync(proxyRequest, new TestLambdaContext());

        response.Should().HaveStatusCodeOrDump((int)expectedStatusCode, allowRichOutput: false);

        var parsedBody = ResponseHelper.ParseJsonBody<TParsedOutput>(response.Body, "lambda");

        bodyAssertions?.Invoke(parsedBody);

        return parsedBody;
    }
}
