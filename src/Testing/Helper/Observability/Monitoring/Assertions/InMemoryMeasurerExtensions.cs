using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions
{
    /// <summary>
    /// Extension methods for <see cref="InMemoryMeasurerAssertions"/>
    /// </summary>
    public static class InMemoryMeasurerExtensions
    {
        /// <summary>
        /// Returns an <see cref="InMemoryMeasurerAssertions"/> object that can be used to assert InMemoryMeasurer
        /// </summary>
        /// <param name="instance"></param>
        /// <returns><see cref="InMemoryMeasurerAssertions"/></returns>
        public static InMemoryMeasurerAssertions Should(this InMemoryMeasurer instance) =>
            new InMemoryMeasurerAssertions(instance, AssertionChain.GetOrCreate());
    }
}
