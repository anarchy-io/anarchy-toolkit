using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health.SelfHost;

public class SelfHostTests : IDisposable
{
    private async Task AssertUrlResponseAsync(
        string url,
        HttpMethod verb,
        HttpStatusCode expectedStatusCode,
        string? expectedContentType,
        Action<string>? contentAssertion
    )
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        _healthApi.Start();

        var request = new HttpRequestMessage(verb, url);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        response.StatusCode.Should()
            .Be(expectedStatusCode);

        var contentType = response.Content.Headers.ContentType?.ToString();
        contentType.Should()
            .Be(expectedContentType);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        contentAssertion?.Invoke(content);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _healthApi.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task HealthInstanceUrlReturnsOkWithoutContent()
    {
        await AssertUrlResponseAsync(
            url: "/health/instance",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: null,
            contentAssertion: content => content.Should()
                .BeEmpty()
        );
    }

    [Fact]
    public async Task HealthServicesUrlReturnsOkWithHealthCheckReportJson()
    {
        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: JsonMimeType,
            contentAssertion: content => content.Should()
                .Contain("\"Passed\":true")
        );
    }

    [Fact]
    public async Task FailingReportsHealthServicesUrlReturnsInternalServerErrorWithHealthCheckReportJson()
    {
        _healthApi = SelfHostedApplicationHealthApi.WithMetadataFromAssembly(
            _portNumber,
            [
                new StubServiceHealthReport("Health test", HealthCheckResult.Failed)
            ],
            WithMetadataHelper.Assembly,
            DateTimeOffset.Now
        );

        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.InternalServerError,
            expectedContentType: JsonMimeType,
            contentAssertion: content => content.Should()
                .Contain("\"Passed\":false")
        );
    }

    [Fact]
    public async Task WithServicesRunsTheScopedReportsOfTheGivenContainer()
    {
        var services = new ServiceCollection();
        services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);
        services.AddScoped<IReadOnlyCollection<IServiceHealthReport>>(_ =>
            [new StubServiceHealthReport("Scoped report")]);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        _healthApi = SelfHostedApplicationHealthApi.WithServices(
            _portNumber, provider.GetRequiredService<IServiceScopeFactory>());

        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: JsonMimeType,
            contentAssertion: content => content.Should()
                .Contain("Scoped report")
        );
    }

    [Fact]
    public async Task HealthStatusUrlReturnsOkWithApplicationInformationJson()
    {
        await AssertUrlResponseAsync(
            url: "/health/status",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: JsonMimeType,
            contentAssertion: content => content.Should()
                .Contain("\"current_time\"")
        );
    }

    [Fact]
    public async Task OtherUrlReturnsNotFound()
    {
        await AssertUrlResponseAsync(
            url: "/something/else",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.NotFound,
            expectedContentType: null,
            contentAssertion: content => content.Should()
                .BeEmpty()
        );
    }

    [Fact]
    public async Task HealthInstanceUrlNonGetVerbReturnsMethodNotAllowed()
    {
        await AssertUrlResponseAsync(
            url: "/health/instance",
            verb: HttpMethod.Post,
            expectedStatusCode: HttpStatusCode.MethodNotAllowed,
            expectedContentType: PlainTextMimeType,
            contentAssertion: content => content.Should()
                .Be(OnlyGetRequestsAllowedMessage)
        );
    }

    [Fact]
    public async Task HealthServicesUrlNonGetVerbReturnsMethodNotAllowed()
    {
        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Post,
            expectedStatusCode: HttpStatusCode.MethodNotAllowed,
            expectedContentType: PlainTextMimeType,
            contentAssertion: content => content.Should()
                .Be(OnlyGetRequestsAllowedMessage)
        );
    }

    [Fact]
    public async Task HealthStatusUrlNonGetVerbReturnsMethodNotAllowed()
    {
        await AssertUrlResponseAsync(
            url: "/health/status",
            verb: HttpMethod.Post,
            expectedStatusCode: HttpStatusCode.MethodNotAllowed,
            expectedContentType: PlainTextMimeType,
            contentAssertion: content => content.Should()
                .Be(OnlyGetRequestsAllowedMessage)
        );
    }

    [Fact]
    public async Task WithCustomMetadataHealthStatusUrlReturnsOkWithApplicationInformationJson()
    {
        _healthApi = SelfHostedApplicationHealthApi.WithCustomMetadata(
            _portNumber,
            [],
            new DummyBuildMetadata(),
            DateTimeOffset.Now
        );

        await AssertUrlResponseAsync(
            url: "/health/status",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: "application/json",
            contentAssertion: content => content.Should()
                .Contain("\"current_time\"")
        );
    }

    [Fact]
    public void CanOnlyStartOnce()
    {
        _healthApi.Start();

        _healthApi.Invoking(obj => obj.Start())
            .Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .ContainEquivalentOf("already started");
    }

    [Fact]
    public void StopBeforeStartDoesNotThrow()
    {
        _healthApi.Invoking(obj => obj.Stop())
            .Should()
            .NotThrow();
    }

    [Fact]
    public async Task StopAfterStartStopsListening()
    {
        _healthApi.Start();
        _healthApi.Stop();

        // The legacy test blocked with GetAsync("/").Wait(); awaited here to satisfy the xUnit analyzers.
        Func<Task> act = () => _httpClient.GetAsync("/", TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task DisposeAfterStartStopsListening()
    {
        _healthApi.Start();
        _healthApi.Dispose();

        Func<Task> act = () => _httpClient.GetAsync("/", TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<HttpRequestException>();
    }

    private const string OnlyGetRequestsAllowedMessage = "Only GET requests are allowed.";
    private const string PlainTextMimeType = "text/plain";
    private const string JsonMimeType = "application/json";

    private readonly int _portNumber = EphemeralPortUtility.FindFreeEphemeralTcpPort();
    private readonly HttpClient _httpClient;

    private SelfHostedApplicationHealthApi _healthApi;

    public SelfHostTests()
    {
        _httpClient = new HttpClient {
            BaseAddress = new Uri($"http://localhost:{_portNumber}")
        };

        _healthApi =
            SelfHostedApplicationHealthApi.WithMetadataFromAssembly(
                portNumber: _portNumber,
                reports: [],
                assembly: WithMetadataHelper.Assembly,
                applicationStartTime: DateTimeOffset.Now
            );
    }

    private class DummyBuildMetadata : IBuildMetadata
    {
        public string ApplicationName { get; } = string.Empty;
        public string ApplicationVersion { get; } = string.Empty;
        public string? TargetFramework { get; } = null;
        public string? BuildNumber { get; } = null;
        public string? BuildMachineName { get; } = null;
        public string? BuildUsername { get; } = null;
        public DateTimeOffset? BuildTimestamp { get; } = null;
        public string? BuildCommitHash { get; } = null;
        public string? RunbookUri { get; } = null;
    }
}
