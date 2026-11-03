using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;
using Moq;
using System.Data;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;

public class DescriptionEnumTypeHandlerTest
{
    private readonly DescriptionEnumTypeHandler<DummyStatus> _sut = new();

    [Theory]
    [InlineData("CREATED", DummyStatus.Created)]
    [InlineData("PARTIALLY_PAID", DummyStatus.PartiallyPaid)]
    [InlineData("PAID", DummyStatus.Paid)]
    [InlineData("PACKED", DummyStatus.Packed)]
    [InlineData("PICKED_UP_BY_CUSTOMER", DummyStatus.PickedUpByCustomer)]
    [InlineData("OUT_FOR_DELIVERY", DummyStatus.OutForDelivery)]
    [InlineData("DELIVERED", DummyStatus.Delivered)]
    [InlineData("DELETED", DummyStatus.Deleted)]
    [InlineData("UNKNOWN-HISTORICAL_DATA", DummyStatus.UnknownHistoricalData)]
    public void Parse_WithValidDescription_ReturnsExpectedEnumValue(string description, DummyStatus expected)
    {
        // Act
        var result = _sut.Parse(description);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Parse_WithInvalidDescription_ThrowsException()
    {
        // Act
        var act = () => _sut.Parse("INVALID_STATUS");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Parse_WithNonStringValue_ThrowsInvalidFormatException()
    {
        // Act
        var act = () => _sut.Parse(42);

        // Assert
        act.Should().Throw<InvalidFormatException>()
            .WithMessage("*Cannot convert*DummyStatus*");
    }

    [Theory]
    [InlineData(DummyStatus.Created, "CREATED")]
    [InlineData(DummyStatus.Paid, "PAID")]
    [InlineData(DummyStatus.Delivered, "DELIVERED")]
    public void SetValue_WithEnumValue_SetsDescriptionAsParameterValue(
        DummyStatus enumValue, string expectedDescription)
    {
        // Arrange
        var mockParameter = new Mock<IDbDataParameter>();

        // Act
        _sut.SetValue(mockParameter.Object, enumValue);

        // Assert
        mockParameter.VerifySet(p => p.Value = expectedDescription);
        mockParameter.VerifySet(p => p.DbType = DbType.String);
    }

    [Fact]
    public void SetValue_WithNullValue_SetsDbNull()
    {
        // Arrange
        var mockParameter = new Mock<IDbDataParameter>();

        // Act
        _sut.SetValue(mockParameter.Object, default);

        // Assert
        mockParameter.VerifySet(p => p.DbType = DbType.String);
    }
}
