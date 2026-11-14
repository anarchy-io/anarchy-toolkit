using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;

public static class InMemorySinkExtensions
{
    public static InMemorySinkAssertions Should(this InMemorySink inMemorySink) =>
        new(inMemorySink, AssertionChain.GetOrCreate());
}
