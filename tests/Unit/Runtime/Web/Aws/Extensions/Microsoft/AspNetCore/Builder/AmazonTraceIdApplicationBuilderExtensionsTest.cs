using Anarchy.Toolkit.Bootstrap.Aws.Core.Service;
using Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module;
using Anarchy.Toolkit.Runtime.Web.Aws.Extensions.Microsoft.AspNetCore.Builder;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Web.Aws.Extensions.Microsoft.AspNetCore.Builder;

public class AmazonTraceIdApplicationBuilderExtensionsTest
{
    [Fact]
    public async Task UseAmazonTraceId_ExposesTheRequestTraceIdAndEchoesIt()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        new ModuleAwsRequestContext().RegisterServices(builder.Services, builder.Configuration, builder.Environment);

        await using var app = builder.Build();
        app.UseAmazonTraceId();
        string? seen = null;
        app.Run(context => {
            seen = context.RequestServices.GetRequiredService<IAmazonRequestContext>().AmazonTraceId;
            return Task.CompletedTask;
        });
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(AmazonTraceIdApplicationBuilderExtensions.TraceIdHeader, "Root=1-abc");
        var response = await app.GetTestClient().SendAsync(request, TestContext.Current.CancellationToken);

        seen.Should().Be("Root=1-abc");
        response.Headers.GetValues(AmazonTraceIdApplicationBuilderExtensions.TraceIdHeader)
            .Should().ContainSingle().Which.Should().Be("Root=1-abc");
    }
}
