using Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build;
using AwesomeAssertions;
using System.ComponentModel;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Core.ValueSet.Build;

public class EnvironmentValueSetTest
{
    [Fact]
    public void ParameterFileValues_AreReadAsEnumerations()
    {
        ((string)JsonSerializer.Deserialize<ConfigurationValueSet>("\"Debug\"")!).Should().Be("Debug");
        ((string)JsonSerializer.Deserialize<EnvironmentValueSet>("\"testing\"")!).Should().Be("Testing");
        ((string)JsonSerializer.Deserialize<RuntimeValueSet>("\"linux-x64\"")!).Should().Be("linux-x64");
        JsonSerializer.Serialize(ConfigurationValueSet.Release).Should().Be("\"Release\"");
    }

    [Fact]
    public void Parse_GivenValueInDifferentCase_ReturnsMatchingEnvironment()
    {
        // Act (how Fallout parses --dotnet_environment / DOTNET_ENVIRONMENT)
        var parsed = (EnvironmentValueSet?)TypeDescriptor.GetConverter(typeof(EnvironmentValueSet)).ConvertFrom("testing");

        // Assert
        (parsed == EnvironmentValueSet.Testing).Should().BeTrue();
        (parsed != EnvironmentValueSet.Testing).Should().BeFalse();
        EnvironmentValueSet.Testing.Equals(parsed).Should().BeTrue();
    }

    [Fact]
    public void Equality_GivenDifferentValues_IsNotEqual()
    {
        // Act & Assert
        (EnvironmentValueSet.Acceptance == EnvironmentValueSet.Production).Should().BeFalse();
    }

    [Fact]
    public void Equality_GivenNulls_ComparesByReference()
    {
        // Arrange
        EnvironmentValueSet? missing = null;

        // Act & Assert
        (missing == null).Should().BeTrue();
        (missing == EnvironmentValueSet.Shared).Should().BeFalse();
        (EnvironmentValueSet.Shared == missing).Should().BeFalse();
    }

    [Fact]
    public void ImplicitString_ReturnsValue()
    {
        // Act
        string value = EnvironmentValueSet.Production;

        // Assert
        value.Should().Be("Production");
    }

    [Fact]
    public void Configuration_ImplicitString_ReturnsValue()
    {
        // Act
        string debug = ConfigurationValueSet.Debug;
        string runtime = RuntimeValueSet.Linux;

        // Assert
        debug.Should().Be("Debug");
        runtime.Should().Be("linux-x64");
    }
}
