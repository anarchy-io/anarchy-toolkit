using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Ui.Presentation.Service;

public class JsonModalStateTest
{
    [Fact]
    public void Open_WithoutAppend_ResetsHistory()
    {
        var state = new JsonModalState();
        state.Open("Order", "{}");

        state.Open("Product", "{}");

        state.IsOpen.Should().BeTrue();
        state.History.Should().ContainSingle().Which.Title.Should().Be("Product");
    }

    [Fact]
    public void NavigateTo_TruncatesHistory()
    {
        // Arrange
        var state = new JsonModalState();
        state.Open("Order", "{}");
        state.Open("Order / Items", "[]", appendToHistory: true);
        state.Open("Order / Items / 1", "{}", appendToHistory: true);

        // Act
        state.NavigateTo(0);

        // Assert
        state.History.Should().ContainSingle();
        state.Title.Should().Be("Order");
    }

    [Fact]
    public void GetHistoryIndexForBreadcrumb_ReturnsLatestEntryWithSegmentCount()
    {
        var state = new JsonModalState();
        state.Open("Order", "{}");
        state.Open("Order / Items", "[]", appendToHistory: true);
        state.Open("Order / Items / 1", "{}", appendToHistory: true);

        state.GetHistoryIndexForBreadcrumb(1).Should().Be(1);
        state.GetHistoryIndexForBreadcrumb(0).Should().Be(0);
    }

    [Fact]
    public void Close_ClearsState()
    {
        var state = new JsonModalState();
        state.Open("Order", "{}", usePreformattedView: true);

        state.Close();

        state.IsOpen.Should().BeFalse();
        state.History.Should().BeEmpty();
        state.UsePreformattedView.Should().BeFalse();
    }

    [Theory]
    [InlineData("""{"id": 42}""", "42")]
    [InlineData("""{"Id": "abc"}""", "abc")]
    [InlineData("""{"name": "x"}""", null)]
    [InlineData("[1]", null)]
    [InlineData("not json", null)]
    public void ExtractIdFromJson_ReadsIdProperty(string json, string? expected)
    {
        JsonModalState.ExtractIdFromJson(json).Should().Be(expected);
    }

    [Fact]
    public void BuildBreadcrumbTitle_JoinsSegmentsAndId()
    {
        JsonModalState.BuildBreadcrumbTitle(["Order", "Items"], "7").Should().Be("Order / Items / 7");
    }
}
