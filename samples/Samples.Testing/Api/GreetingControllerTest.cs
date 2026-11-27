using Anarchy.Toolkit.Samples.Api.Composition;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Fixture.Amazon.Lambda.AspNetCoreServer;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Anarchy.Toolkit.Samples.Testing.Api;

/// <summary>
/// Exercises the example API through both the native (Kestrel/TestServer) and the AWS Lambda pipelines.
/// </summary>
public class GreetingControllerTest(WebApplicationFactory<SampleApiStartup> factory)
    : AwsRestApiTestCaseBase<SampleApiStartup, AppSettings>(new TestableLambdaEntrypoint<SampleApiStartup>(), factory, ["samples:read"])
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
