using Anarchy.Toolkit.Samples.Job;
using Anarchy.Toolkit.Samples.Testing.TestingUtilities;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Samples.Testing.Job;

[Collection(EnvironmentVariableCollection.Name)]
public class FunctionTest
{
    [Fact]
    public async Task LocalJob_DoesNotLoadLambdaParameterStore()
    {
        using var _ = new EnvironmentVariableContext("DOTNET_ENVIRONMENT", "Production");
        using var __ = new EnvironmentVariableContext("Greeting", "Hi");
        await using var job = new EntryPointLocal.Job();

        (await job.GreetAsync("Ada")).Should().Be("Hi, Ada!");
    }

    [Fact]
    public async Task ScheduledHandler_UsesTheSameConfiguredJob()
    {
        using var _ = new EnvironmentVariableContext("DOTNET_ENVIRONMENT", "Development");
        using var __ = new EnvironmentVariableContext("Greeting", "Hi");
        await using var handler = new EntryPointScheduled();
        await handler.HandleAsync();
    }
}
