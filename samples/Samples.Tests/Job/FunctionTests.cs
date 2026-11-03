using Anarchy.Toolkit.Samples.Job;
using Anarchy.Toolkit.Samples.Tests.TestingUtilities;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Samples.Tests.Job;

[Collection(EnvironmentVariableCollection.Name)]
public class FunctionTests
{
    [Fact]
    public async Task HandleAsync_GreetsWithTheGreetingFromTheEnvironment()
    {
        // Development keeps the Parameter Store out of the way; the greeting comes from an environment variable
        using var _ = new EnvironmentVariableContext("DOTNET_ENVIRONMENT", "Development");
        using var __ = new EnvironmentVariableContext("Greeting", "Hi");
        await using var function = new Function();

        var result = await function.HandleAsync("Ada");

        result.Should().Be("Hi, Ada!");
    }
}
