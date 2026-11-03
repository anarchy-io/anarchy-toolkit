using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Helper;

public class DateRangeHelperTest : TestCaseBase
{
    #region IsWithinRange Tests

    [Fact]
    public void IsWithinRange_WhenTimeIsWithinRange_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-1);
        var to = now.AddDays(1);

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to, now);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinRange_WhenToIsNull_AndAfterFrom_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-1);

        // Act
        var result = DateRangeHelper.IsWithinRange(from, null, now);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinRange_WhenBeforeFrom_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(1);
        var to = now.AddDays(2);

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to, now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsWithinRange_WhenAfterTo_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-2);
        var to = now.AddDays(-1);

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to, now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsWithinRange_WhenExactlyAtFrom_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now;
        var to = now.AddDays(1);

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to, now);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinRange_WhenExactlyAtTo_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-1);
        var to = now;

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to, now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsWithinRange_WhenNoTimeProvided_UsesCurrentTime()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinRange_TruncatesToMilliseconds_IgnoresMicrosecondDifferences()
    {
        // Arrange - Create dates with microsecond differences that should be equal after truncation
        var baseTime = new DateTimeOffset(2024, 6, 15, 10, 30, 45, 123, TimeSpan.Zero);
        var from = baseTime;
        var to = baseTime.AddDays(1);
        var atWithMicroseconds = baseTime.AddTicks(4567); // Same millisecond, different microseconds

        // Act
        var result = DateRangeHelper.IsWithinRange(from, to, atWithMicroseconds);

        // Assert - Should be true because after truncation, 'at' equals 'from'
        result.Should().BeTrue();
    }

    #endregion

    #region DoDateRangesOverlap Tests

    [Fact]
    public void DoDateRangesOverlap_WhenRangesOverlap_ReturnsTrue()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range1To = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var range2From = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var range2To = new DateTimeOffset(2024, 9, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DoDateRangesOverlap_WhenRangesDoNotOverlap_ReturnsFalse()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range1To = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var range2From = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var range2To = new DateTimeOffset(2024, 9, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DoDateRangesOverlap_WhenRange1ContainsRange2_ReturnsTrue()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range1To = new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var range2From = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var range2To = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DoDateRangesOverlap_WhenRange1ToIsNull_AndRangesOverlap_ReturnsTrue()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset? range1To = null;
        var range2From = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var range2To = new DateTimeOffset(2024, 9, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DoDateRangesOverlap_WhenRange2ToIsNull_AndRangesOverlap_ReturnsTrue()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range1To = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var range2From = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset? range2To = null;

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DoDateRangesOverlap_WhenBothRangesToAreNull_ReturnsTrue()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset? range1To = null;
        var range2From = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset? range2To = null;

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DoDateRangesOverlap_WhenRangesTouchAtBoundary_ReturnsTrue()
    {
        // Arrange
        var range1From = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var range1To = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var range2From = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var range2To = new DateTimeOffset(2024, 9, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = DateRangeHelper.DoDateRangesOverlap(range1From, range1To, range2From, range2To);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region TruncateToMilliseconds Tests

    [Fact]
    public void TruncateToMilliseconds_RemovesMicroseconds()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2024, 6, 15, 10, 30, 45, 123, TimeSpan.Zero).AddTicks(4567);

        // Act
        var result = DateRangeHelper.TruncateToMilliseconds(dateTime);

        // Assert
        result.Year.Should().Be(2024);
        result.Month.Should().Be(6);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(10);
        result.Minute.Should().Be(30);
        result.Second.Should().Be(45);
        result.Millisecond.Should().Be(123);
        (result.Ticks % TimeSpan.TicksPerMillisecond).Should().Be(0);
    }

    [Fact]
    public void TruncateToMilliseconds_PreservesOffset()
    {
        // Arrange
        var offset = TimeSpan.FromHours(2);
        var dateTime = new DateTimeOffset(2024, 6, 15, 10, 30, 45, 123, offset);

        // Act
        var result = DateRangeHelper.TruncateToMilliseconds(dateTime);

        // Assert
        result.Offset.Should().Be(offset);
    }

    #endregion
}

