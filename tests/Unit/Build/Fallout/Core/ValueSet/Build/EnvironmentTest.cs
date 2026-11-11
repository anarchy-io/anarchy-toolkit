using Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build;
using AwesomeAssertions;
using System.ComponentModel;
using System.Text.Json;
using BuildConfiguration = Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build.Configuration;
using Environment = Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build.Environment;

namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Core.ValueSet.Build;

public class EnvironmentTest
{
    [Fact]
    public void ParameterFileValues_AreReadAsEnumerations()
    {
        ((string)JsonSerializer.Deserialize<BuildConfiguration>("\"Debug\"")!).Should().Be("Debug");
        ((string)JsonSerializer.Deserialize<Environment>("\"testing\"")!).Should().Be("Testing");
        ((string)JsonSerializer.Deserialize<Runtime>("\"linux-x64\"")!).Should().Be("linux-x64");
        JsonSerializer.Serialize(BuildConfiguration.Release).Should().Be("\"Release\"");
    }

    [Fact]
    public void Parse_GivenValueInDifferentCase_ReturnsMatchingEnvironment()
    {
        // Act (how Fallout parses --dotnet_environment / DOTNET_ENVIRONMENT)
        var parsed = (Environment?)TypeDescriptor.GetConverter(typeof(Environment)).ConvertFrom("testing");

        // Assert
        (parsed == Environment.Testing).Should().BeTrue();
        (parsed != Environment.Testing).Should().BeFalse();
        Environment.Testing.Equals(parsed).Should().BeTrue();
    }

    [Fact]
    public void Equality_GivenDifferentValues_IsNotEqual()
    {
        // Act & Assert
        (Environment.Acceptance == Environment.Production).Should().BeFalse();
    }

    [Fact]
    public void Equality_GivenNulls_ComparesByReference()
    {
        // Arrange
        Environment? missing = null;

        // Act & Assert
        (missing == null).Should().BeTrue();
        (missing == Environment.Shared).Should().BeFalse();
        (Environment.Shared == missing).Should().BeFalse();
    }

    [Fact]
    public void ImplicitString_ReturnsValue()
    {
        // Act
        string value = Environment.Production;

        // Assert
        value.Should().Be("Production");
    }

    [Fact]
    public void Configuration_ImplicitString_ReturnsValue()
    {
        // Act
        string debug = Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build.Configuration.Debug;
        string runtime = Runtime.Linux;

        // Assert
        debug.Should().Be("Debug");
        runtime.Should().Be("linux-x64");
    }
}
