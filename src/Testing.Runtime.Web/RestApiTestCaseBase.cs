using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Runtime.Web.Composition;
using Anarchy.Toolkit.Runtime.Web.Helpers.Presentation.Contract;
using Anarchy.Toolkit.Testing.Helpers.Http;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using Anarchy.Toolkit.Testing.Runtime.Web.Extensions.System.Net.Http;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Anarchy.Toolkit.Testing.Runtime.Web;

/// <summary>
/// Base class for end-to-end tests of REST APIs hosted as native ASP.NET Core applications (via
/// <see cref="WebApplicationFactory{TStartup}"/>), calling the API through a test <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// Implements xUnit's <c>IClassFixture</c> for <see cref="WebApplicationFactory{TEntryPoint}"/>, so the factory (and
/// <see cref="Client"/>'s server) is shared by all tests of a class. Request routes are resolved with
/// <see cref="HttpRouteHelper.FromTemplate"/> relative to <paramref name="controllerRouteBaseUrl"/>. APIs also deployed
/// as AWS Lambda functions derive from <c>AwsRestApiTestCaseBase</c> (<c>Anarchy.Toolkit.Testing.Runtime.Web.Aws</c>)
/// instead, whose <see cref="TestEndpointAsync{TParsedOutput}"/> exercises both hosting paths.
/// </remarks>
/// <typeparam name="TStartup">The application startup type.</typeparam>
/// <typeparam name="TAppSettings">
/// The consumer's concrete application settings type. When it implements <see cref="IAuthServiceAppSettings"/>, its
/// test settings provide the mock token issuer; otherwise no token is added.
/// </typeparam>
/// <param name="factory">The web-application factory used to host the native pipeline.</param>
/// <param name="authorizationScopes">
/// The OAuth2 scope(s) embedded in the mock bearer token when
/// <c>includeAuthorizationToken</c> is <c>true</c>. Consumers supply their own scope strings.
/// </param>
/// <param name="controllerRouteBaseUrl">Optional base URL prepended to request routes.</param>
/// <example>
/// <code language="csharp">
/// public class OrdersApiTest(WebApplicationFactory&lt;Startup&gt; factory)
///     : RestApiTestCaseBase&lt;Startup, AppSettings&gt;(factory, ["orders:read"], "api/orders")
/// {
///     [Fact]
///     public async Task GetOrder_ReturnsOk() =&gt;
///         await TestEndpointAsync&lt;OrderResponse&gt;(
///             Get("{id}", 42), HttpStatusCode.OK,
///             body =&gt; body!.Id.Should().Be(42));
/// }
/// </code>
/// </example>
public abstract class RestApiTestCaseBase<TStartup, TAppSettings>(
    WebApplicationFactory<TStartup> factory,
    string[] authorizationScopes,
    string? controllerRouteBaseUrl = null)
    : IClassFixture<WebApplicationFactory<TStartup>>
    where TStartup : class, IWebAppStartup
    where TAppSettings : class, IAppSettings
{
    /// <summary>
    /// An <see cref="HttpClient"/> targeting the in-memory test server created by the <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    protected readonly HttpClient Client = factory.CreateClient();

    /// <summary>
    /// The OAuth2 scope(s) embedded in the mock bearer token when <c>includeAuthorizationToken</c> is <c>true</c>.
    /// </summary>
    protected string[] AuthorizationScopes { get; } = authorizationScopes;

    /// <summary>Creates a <c>POST</c> request for the given route template (see <see cref="Request"/>).</summary>
    /// <param name="route">The route template, relative to the controller base URL unless it starts with <c>/</c>.</param>
    /// <param name="routeParameters">Values substituted into the route template.</param>
    /// <returns>A new request message.</returns>
    protected HttpRequestMessage Post(string route, params object[] routeParameters) =>
        Request(HttpMethod.Post, route, routeParameters);

    /// <summary>Creates a <c>GET</c> request for the given route template (see <see cref="Request"/>).</summary>
    /// <param name="route">The route template, relative to the controller base URL unless it starts with <c>/</c>.</param>
    /// <param name="routeParameters">Values substituted into the route template.</param>
    /// <returns>A new request message.</returns>
    protected HttpRequestMessage Get(string route, params object[] routeParameters) =>
        Request(HttpMethod.Get, route, routeParameters);

    /// <summary>Creates a <c>PUT</c> request for the given route template (see <see cref="Request"/>).</summary>
    /// <param name="route">The route template, relative to the controller base URL unless it starts with <c>/</c>.</param>
    /// <param name="routeParameters">Values substituted into the route template.</param>
    /// <returns>A new request message.</returns>
    protected HttpRequestMessage Put(string route, params object[] routeParameters) =>
        Request(HttpMethod.Put, route, routeParameters);

    /// <summary>Creates a <c>PATCH</c> request for the given route template (see <see cref="Request"/>).</summary>
    /// <param name="route">The route template, relative to the controller base URL unless it starts with <c>/</c>.</param>
    /// <param name="routeParameters">Values substituted into the route template.</param>
    /// <returns>A new request message.</returns>
    protected HttpRequestMessage Patch(string route, params object[] routeParameters) =>
        Request(HttpMethod.Patch, route, routeParameters);

    /// <summary>
    /// Creates a request with the given method, resolving the route with <see cref="HttpRouteHelper.FromTemplate"/>
    /// against the controller base URL. Add a body with
    /// <c>WithJsonPayload</c>.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="route">The route template, relative to the controller base URL unless it starts with <c>/</c>.</param>
    /// <param name="routeParameters">Positional values, or a single object/dictionary of named values, substituted into the route.</param>
    /// <returns>A new request message.</returns>
    protected HttpRequestMessage Request(HttpMethod method, string route, params object[] routeParameters) =>
        new(method, HttpRouteHelper.FromTemplate(route, controllerRouteBaseUrl, routeParameters));

    /// <summary>
    /// For endpoints with no response body: executes the request on every hosting path of this test base and asserts
    /// the status code (see <see cref="TestEndpointAsync{TParsedOutput}"/>).
    /// </summary>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true); ignored when the settings have no auth service</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes when every execution has been asserted.</returns>
    protected async Task TestEndpointAsync(HttpRequestMessage requestFixture, HttpStatusCode expectedStatusCode,
                                           bool includeAuthorizationToken = true,
                                           CancellationToken cancellationToken = default)
    {
        await TestEndpointAsync<object>(requestFixture, expectedStatusCode,
            includeAuthorizationToken: includeAuthorizationToken,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes the request on every hosting path of this test base, asserts the status code and applies the provided
    /// body assertions. Here it runs the native path (<see cref="TestEndpointNativeAsync{TParsedOutput}"/>);
    /// derived test bases add other hosting paths.
    /// </summary>
    /// <typeparam name="TParsedOutput">The expected response model type.</typeparam>
    /// <param name="requestFixture">Pre-constructed HTTP request to send.</param>
    /// <param name="expectedStatusCode">Expected HTTP status code of the response.</param>
    /// <param name="bodyAssertions">Optional domain-specific assertions on the deserialized body.</param>
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true); ignored when the settings have no auth service</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body (or <c>null</c> if empty).</returns>
    protected virtual Task<TParsedOutput?> TestEndpointAsync<TParsedOutput>(
        HttpRequestMessage requestFixture,
        HttpStatusCode expectedStatusCode,
        Action<TParsedOutput?>? bodyAssertions = null,
        bool includeAuthorizationToken = true,
        CancellationToken cancellationToken = default) where TParsedOutput : class
    {
        return TestEndpointNativeAsync(requestFixture, expectedStatusCode, bodyAssertions, includeAuthorizationToken,
            cancellationToken);
    }

    /// <summary>
    /// Adds a mock bearer token with <see cref="AuthorizationScopes"/> when the test settings provide an auth service;
    /// does nothing otherwise, so APIs without authentication can use this base too.
    /// </summary>
    /// <param name="request">The request to authorize.</param>
    protected void AddMockAuthorizationToken(HttpRequestMessage request)
    {
        if (TestHelper.TestSettings<TAppSettings>() is IAuthServiceAppSettings settings)
        {
            request.MockAuthorizationToken<IAuthServiceAppSettings>(
                AuthorizationScopes, issuer: settings.AuthService.Host.ToString());
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
    /// <param name="includeAuthorizationToken">Adds an OAuth2 Token to the request's header (default: true); ignored when the settings have no auth service</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed response body (or <c>null</c> if empty).</returns>
    protected async Task<TParsedOutput?> TestEndpointNativeAsync<TParsedOutput>(
        HttpRequestMessage requestFixture,
        HttpStatusCode expectedStatusCode,
        Action<TParsedOutput?>? bodyAssertions = null,
        bool includeAuthorizationToken = true,
        CancellationToken cancellationToken = default) where TParsedOutput : class
    {
        if (includeAuthorizationToken) AddMockAuthorizationToken(requestFixture);

        var response = await Client.SendAsync(requestFixture, cancellationToken);
        response.StatusCode.Should().Be(expectedStatusCode);

        var parsedBody = await ResponseHelper.ParseHttpResponse<TParsedOutput>(response, cancellationToken);

        bodyAssertions?.Invoke(parsedBody);

        return parsedBody;
    }
}
