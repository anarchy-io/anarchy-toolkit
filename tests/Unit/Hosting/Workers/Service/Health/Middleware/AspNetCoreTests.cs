using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.AspNetCore;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Mime;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health.Middleware;

public class AspNetCoreTests : AspNetCoreBaseTests
{

    [Fact]
    public async Task HealthInstanceUrlReturnsOkWithoutContent()
    {
        await AssertUrlResponseAsync(
            url: "/health/instance",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: null,
            contentAssertion: content => content.Should().BeEmpty()
        );
    }

    [Fact]
    public async Task HealthServicesUrlReturnsOkWithHealthCheckReportJson()
    {
        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: MediaTypeNames.Application.Json,
            contentAssertion: content => content.Should().Contain("\"Passed\":true")
        );
    }

    [Fact]
    public async Task FailingReportsHealthServicesUrlReturnsInternalServerErrorWithHealthCheckReportJson()
    {
        await CreateTestServerWithFailingTest();

        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.InternalServerError,
            expectedContentType: MediaTypeNames.Application.Json,
            contentAssertion: content => content.Should().Contain("\"Passed\":false")
        );
    }

    [Fact]
    public async Task HealthStatusUrlReturnsOkWithApplicationInformationJson()
    {
        await AssertUrlResponseAsync(
            url: "/health/status",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: MediaTypeNames.Application.Json,
            contentAssertion: content => content.Should().Contain("\"current_time\"")
        );
    }

    [Fact]
    public async Task OtherUrlReturnsDefaultContent()
    {
        await AssertUrlResponseAsync(
            url: "/something/else",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: MediaTypeNames.Text.Plain,
            contentAssertion: content => content.Should().Be(DefaultResponseText)
        );
    }

    [Fact]
    public async Task HealthInstanceUrlNonGetVerbReturnsMethodNotAllowed()
    {
        await AssertUrlResponseAsync(
            url: "/health/instance",
            verb: HttpMethod.Post,
            expectedStatusCode: HttpStatusCode.MethodNotAllowed,
            expectedContentType: MediaTypeNames.Text.Plain,
            contentAssertion: content => content.Should().Be(OnlyGetRequestsAllowedMessage)
        );
    }

    [Fact]
    public async Task HealthServicesUrlNonGetVerbReturnsMethodNotAllowed()
    {
        await AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Post,
            expectedStatusCode: HttpStatusCode.MethodNotAllowed,
            expectedContentType: MediaTypeNames.Text.Plain,
            contentAssertion: content => content.Should().Be(OnlyGetRequestsAllowedMessage)
        );
    }

    [Fact]
    public async Task HealthStatusUrlNonGetVerbReturnsMethodNotAllowed()
    {
        await AssertUrlResponseAsync(
            url: "/health/status",
            verb: HttpMethod.Post,
            expectedStatusCode: HttpStatusCode.MethodNotAllowed,
            expectedContentType: MediaTypeNames.Text.Plain,
            contentAssertion: content => content.Should().Be(OnlyGetRequestsAllowedMessage)
        );
    }

    [Fact]
    public async Task WithCustomMetadataHealthStatusUrlReturnsOkWithApplicationInformationJson()
    {
        await CreateTestServerWithCustomMetadata();

        await AssertUrlResponseAsync(
            url: "/health/status",
            verb: HttpMethod.Get,
            expectedStatusCode: HttpStatusCode.OK,
            expectedContentType: MediaTypeNames.Application.Json,
            contentAssertion: content => content.Should().Contain("\"current_time\"")
        );
    }

    private Task CreateTestServerWithCustomMetadata() => CreateTestServer<CustomMetadataTestServerStartup>();

    private Task CreateTestServerWithFailingTest() => CreateTestServer<FailingTestsTestServerStartup>();

    private class FailingTestsTestServerStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IReadOnlyCollection<IServiceHealthReport>>(
                [new StubServiceHealthReport("Health test", HealthCheckResult.Failed)]);
            services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);

            base.ConfigureServices(services);
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseApplicationHealthEndpoints();

            base.Configure(app);
        }
    }

    private class CustomMetadataTestServerStartup : StartupBase
    {
        public override void Configure(IApplicationBuilder app)
        {
            app.UseApplicationHealthEndpointsWithCustomMetadata(
                Array.Empty<IServiceHealthReport>(),
                new DummyBuildMetadata(),
                DateTimeOffset.Now
            );

            base.Configure(app);
        }
    }

    private class DummyBuildMetadata : IBuildMetadata
    {
        public string ApplicationName { get; } = string.Empty;
        public string ApplicationVersion { get; } = string.Empty;
        public string? TargetFramework { get; }
        public string? BuildNumber { get; }
        public string? BuildMachineName { get; }
        public string? BuildUsername { get; }
        public DateTimeOffset? BuildTimestamp { get; }
        public string? BuildCommitHash { get; }
        public string? RunbookUri { get; }
    }
}
