using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;

/// <summary>
/// Entry point extensions for asserting on an <see cref="InMemorySink"/>.
/// </summary>
public static class InMemorySinkExtensions
{
    /// <summary>
    /// Returns an <see cref="InMemorySinkAssertions"/> object that can be used to assert on captured log events.
    /// </summary>
    /// <param name="inMemorySink">The sink under test.</param>
    /// <returns>An <see cref="InMemorySinkAssertions"/> bound to the current <see cref="AssertionChain"/>.</returns>
    public static InMemorySinkAssertions Should(this InMemorySink inMemorySink) =>
        new(inMemorySink, AssertionChain.GetOrCreate());
}
