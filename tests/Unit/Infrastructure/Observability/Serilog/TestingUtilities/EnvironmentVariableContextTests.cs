using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Observability.Serilog.TestingUtilities;

[Collection(EnvironmentVariableCollection.Name)]
public class EnvironmentVariableContextTests
{
    [Fact]
    public void GetEnvironmentVariable_ReturnsValue_WhenVariableExists()
    {
        const string variableName = "SOME_TEST_VARIABLE";

        Environment.GetEnvironmentVariable(variableName).Should().BeNull("Because the environment variable should not exist before the test runs.");

        const string expectedValue = "TestValue";
        using (new EnvironmentVariableContext(variableName, expectedValue))
        {
            Environment.GetEnvironmentVariable(variableName).Should().Be(expectedValue, "Because the environment variable be defined within the scope of 'EnvironmentVariableContext'.");

            const string anotherExpectedValue = "AnotherTestValue";
            using (new EnvironmentVariableContext(variableName, anotherExpectedValue))
            {
                Environment.GetEnvironmentVariable(variableName).Should().Be(anotherExpectedValue, "Because nesting 'EnvironmentVariableContext' overrides the override.");
            }

            Environment.GetEnvironmentVariable(variableName).Should().Be(expectedValue, "Because leaving the scope of the nested 'EnvironmentVariableContext' resets back to the original override.");
        }

        Environment.GetEnvironmentVariable(variableName).Should().BeNull("Because the environment variable should not exist after disposing 'EnvironmentVariableContext'.");
    }
}
