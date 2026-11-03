using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Destructuring;
using AwesomeAssertions;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.Destructuring;

public class ObjectTestDestructuringPolicyTests
{
    [Fact]
    public void TryDestructure_GivenObject_ReturnsScalarValueWithObject()
    {
        var expected = new object();
        var sut = new ObjectTestDestructuringPolicy();

        sut.TryDestructure(expected, propertyValueFactory: null!, out var result);

        result.Should().BeOfType<ScalarValue>()
              .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public void TryDestructure_GivenNull_ReturnsScalarValueWithNull()
    {
        var sut = new ObjectTestDestructuringPolicy();

        sut.TryDestructure(value: null!, propertyValueFactory: null!, out var result);

        result.Should().BeOfType<ScalarValue>()
              .Which.Value.Should().BeNull();
    }

    [Fact]
    public void TryDestructure_ReturnsTrue()
    {
        var sut = new ObjectTestDestructuringPolicy();

        var result = sut.TryDestructure(new object(), propertyValueFactory: null!, out _);

        result.Should().BeTrue();
    }
}
