using Anarchy.Toolkit.Samples.Api.Composition;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Testing.Web;
using Anarchy.Toolkit.Testing.Web.Fixture.Amazon.Lambda.AspNetCoreServer;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Anarchy.Toolkit.Samples.Tests.Api;

/// <summary>
/// Exercises the example API through both the native (Kestrel/TestServer) and the AWS Lambda pipelines.
/// </summary>
public class GreetingControllerTests(WebApplicationFactory<Startup> factory)
    : RestApiTestCaseBase<Startup, AppSettings>(new TestableLambdaEntrypoint<Startup>(), factory, ["samples:read"])
{
    public sealed record GreetingOutput(string Message);

    [Fact]
    public async Task Greet_ReturnsTheConfiguredGreeting()
    {
        await TestEndpointAsync<GreetingOutput>(
            Get("greetings/{name}", "Ada"),
            HttpStatusCode.OK,
            output => output!.Message.Should().Be("Hello, Ada!"),
            includeAuthorizationToken: false,
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
