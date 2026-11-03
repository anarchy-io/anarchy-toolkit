using Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build;
using AwesomeAssertions;
using System.ComponentModel;
using Environment = Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build.Environment;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Core.ValueSet.Build;

public class EnvironmentTest
{
    [Fact]
    public void Parse_GivenValueInDifferentCase_ReturnsMatchingEnvironment()
    {
        // Act (how NUKE parses --dotnet_environment / DOTNET_ENVIRONMENT)
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
        string debug = Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build.Configuration.Debug;
        string runtime = Runtime.Linux;

        // Assert
        debug.Should().Be("Debug");
        runtime.Should().Be("linux-x64");
    }
}
