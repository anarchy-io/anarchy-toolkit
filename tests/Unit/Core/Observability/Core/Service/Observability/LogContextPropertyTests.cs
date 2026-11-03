using Anarchy.Toolkit.Core.Observability.Service.Observability;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.Core.Service.Observability;

public class LogContextPropertyTests
{
    [Fact]
    public void CreateWithNullNameShouldThrow()
    {
        Action create = () => new LogContextProperty(name: null!, value: "Value");

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateWithEmptyNameShouldThrow()
    {
        Action create = () => new LogContextProperty(name: "", value: "Value");

        create.Should().Throw<ArgumentException>()
              .Which.Message.Should()
              .ContainEquivalentOf("property name cannot be empty");
    }

    [Fact]
    public void CreateAssignsProperties()
    {
        var sut = new LogContextProperty(name: "Property", value: 1234, destructureObjects: true);

        sut.Name.Should().Be("Property");
        sut.Value.Should().Be(1234);
        sut.DestructureObjects.Should().BeTrue();
    }
}
