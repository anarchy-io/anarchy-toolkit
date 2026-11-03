using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Helper;

public class DateHelperTest : TestCaseBase
{
    [Fact]
    public void SanitiseDate_ReturnsValidDateTimeOffset()
    {
        // Arrange
        var date = new DateTimeOffset(2024, 3, 2, 1, 10, 2, new TimeSpan(1, 0, 0));

        // Act
        var result = DateHelper.SanitiseDate(date);

        // Assert
        result.Day.Should().Be(2);
        result.Month.Should().Be(3);
        result.Year.Should().Be(2024);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
        result.Millisecond.Should().Be(0);
    }

    [Fact]
    public void SanitiseDate_WhenNullInput_ReturnsNull()
    {
        // Act
        var result = DateHelper.SanitiseDate(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCentralEuropeanTimeZoneInfo_ReturnsValidCEST()
    {
        // Act
        var result = DateHelper.GetCentralEuropeanTimeZoneInfo();

        // Assert
        result.Id.Should().ContainAny("Europe/Amsterdam", "Central European Standard Time");
    }

    [Fact]
    public void GetCentralEuropeanOffSet_ReturnsValidOffsets()
    {
        // Act
        var winterTime = DateHelper.GetCentralEuropeanOffSet(new DateTime(2024, 1, 1));
        var summerTime = DateHelper.GetCentralEuropeanOffSet(new DateTime(2024, 4, 1));

        // Assert
        winterTime.Should().Be(TimeSpan.FromHours(1));
        summerTime.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void ConvertToCentralEuropeanOffset_WhenNullInput_ReturnsNull()
    {
        // Arrange
        DateTimeOffset? dateTimeOffset = null;
        DateTime? dateTime = null;


        // Act
        var resultDateTime = DateHelper.ConvertToCentralEuropeanOffset(dateTime);
        var resultDateTimeOffset = DateHelper.ConvertToCentralEuropeanOffset(dateTimeOffset);

        // Assert
        resultDateTime.Should().BeNull();
        resultDateTimeOffset.Should().BeNull();
    }

    [Fact]
    public void ConvertToCentralEuropeanOffset_ReturnsValidOffset()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2024, 1, 1, 10, 56, 3, TimeSpan.Zero);
        var dateTime = new DateTime(2024, 1, 1, 10, 56, 3);


        // Act
        var resultDateTime = DateHelper.ConvertToCentralEuropeanOffset(dateTime);
        var resultDateTimeOffset = DateHelper.ConvertToCentralEuropeanOffset(dateTimeOffset);

        // Assert
        resultDateTime.Should().Be(new DateTimeOffset(2024, 1, 1, 10, 56, 3, TimeSpan.FromHours(1)));
        resultDateTimeOffset.Should().Be(new DateTimeOffset(2024, 1, 1, 10, 56, 3, TimeSpan.FromHours(1)));
    }
}
