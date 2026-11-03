using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.AspNetCore;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health.Middleware;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Mime;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Extension.Microsoft.DependencyInjection;

public class AspNetCoreMicrosoftDiTests : AspNetCoreBaseTests
{
    private Task AssertHealthServicesResponseAsync(HttpStatusCode expectedStatusCode, params string[] expectedNames) =>
        AssertUrlResponseAsync(
            url: "/health/services",
            verb: HttpMethod.Get,
            expectedStatusCode: expectedStatusCode,
            expectedContentType: MediaTypeNames.Application.Json,
            contentAssertion: content => {
                foreach (var name in expectedNames)
                    content.Should().Contain(name);
            }
        );

    [Fact]
    public async Task HealthServicesReturnsOkWithSingleReport()
    {
        await CreateTestServer<SingleReportStartup>();
        await AssertHealthServicesResponseAsync(HttpStatusCode.OK, "Dummy Health test");
    }

    [Fact]
    public async Task HealthServicesReturnsOkWithMultipleReports()
    {
        await CreateTestServer<MultipleReportsStartup>();
        await AssertHealthServicesResponseAsync(HttpStatusCode.OK, "Dummy Health test", "Second");
    }

    [Fact]
    public async Task HealthServicesReturnsOkWithScopedReports()
    {
        await CreateTestServer<ScopedReportsStartup>();
        await AssertHealthServicesResponseAsync(HttpStatusCode.OK, "Dummy Health test", "Second");
    }

    [Fact]
    public async Task HealthServicesUsesReportsRegisteredBeforeTheEndpoints()
    {
        await CreateTestServer<ReportsBeforeEndpointsStartup>();
        await AssertHealthServicesResponseAsync(HttpStatusCode.OK, "Dummy Health test");
    }

    [Fact]
    public async Task HealthServicesReturnsInternalServerErrorWhenAReportFails()
    {
        await CreateTestServer<FailingReportStartup>();
        await AssertHealthServicesResponseAsync(HttpStatusCode.InternalServerError, "\"Passed\":false");
    }

    [Fact]
    public async Task HealthServicesUrlReturnOkWithNoReports()
    {
        await CreateTestServer<NoReportsStartup>();
        await AssertHealthServicesResponseAsync(HttpStatusCode.OK, "\"Tests\":[]");
    }

    [Fact]
    public async Task HealthServicesThrowsExceptionWhenWrongUsageOfDi()
    {
        var createFunc = CreateTestServer<WrongDiStartup>;
        await createFunc.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void AddServiceHealthCheckWorkerRegistersTheWorkerAndTheHealthApi()
    {
        var services = new ServiceCollection();

        services.AddServiceHealthCheckWorker(WithMetadataHelper.Assembly);

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IHostedService)
                                             && descriptor.ImplementationType == typeof(ServiceHealthCheckWorker));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ApplicationHealthApi));
    }

    private abstract class HealthEndpointsStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);
            RegisterReports(services);

            base.ConfigureServices(services);
        }

        protected virtual void RegisterReports(IServiceCollection services) { }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseApplicationHealthEndpoints();

            base.Configure(app);
        }
    }

    private class NoReportsStartup : HealthEndpointsStartup;

    private class SingleReportStartup : HealthEndpointsStartup
    {
        protected override void RegisterReports(IServiceCollection services) =>
            services.AddSingleton<IReadOnlyCollection<IServiceHealthReport>>([new StubServiceHealthReport("Dummy Health test")]);
    }

    private class MultipleReportsStartup : HealthEndpointsStartup
    {
        protected override void RegisterReports(IServiceCollection services) =>
            services.AddSingleton<IReadOnlyCollection<IServiceHealthReport>>([
                new StubServiceHealthReport("Dummy Health test"),
                new StubServiceHealthReport("Second dummy Health test"),
            ]);
    }

    // The registration shape of AddDbHealthReports: scoped reports exposed as one scoped collection.
    private class ScopedReportsStartup : HealthEndpointsStartup
    {
        protected override void RegisterReports(IServiceCollection services) =>
            services.AddScoped<FirstReport>()
                    .AddScoped<SecondReport>()
                    .AddScoped<IReadOnlyCollection<IServiceHealthReport>>(sp => [
                        sp.GetRequiredService<FirstReport>(),
                        sp.GetRequiredService<SecondReport>(),
                    ]);
    }

    private class ReportsBeforeEndpointsStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IReadOnlyCollection<IServiceHealthReport>>([new StubServiceHealthReport("Dummy Health test")]);
            services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);

            base.ConfigureServices(services);
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseApplicationHealthEndpoints();

            base.Configure(app);
        }
    }

    private class FailingReportStartup : HealthEndpointsStartup
    {
        protected override void RegisterReports(IServiceCollection services) =>
            services.AddSingleton<IReadOnlyCollection<IServiceHealthReport>>(
                [new StubServiceHealthReport("Dummy Health test", HealthCheckResult.Failed)]);
    }

    // A singleton report depending on a scoped service is rejected by scope validation (Development environment).
    private class WrongDiStartup : HealthEndpointsStartup
    {
        protected override void RegisterReports(IServiceCollection services) =>
            services.AddScoped<IDummyDiObject, DummyDiObject>()
                    .AddSingleton<DummyDiReport>();
    }

    private class FirstReport() : StubServiceHealthReport("Dummy Health test");

    private class SecondReport() : StubServiceHealthReport("Second dummy Health test");

    private class DummyDiReport(IDummyDiObject obj) : IServiceHealthReport
    {
        public Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null) =>
            Task.FromResult(new HealthCheckEntity($"DI Dummy Health test {obj}", HealthCheckResult.Passed, TimeSpan.Zero));
    }

    private interface IDummyDiObject;

    private class DummyDiObject : IDummyDiObject;
}
