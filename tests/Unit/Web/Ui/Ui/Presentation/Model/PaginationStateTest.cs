using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Model;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.Ui.Ui.Presentation.Model;

public class PaginationStateTest
{
    private static PaginationState WithItems(int totalItems, int pageSize = 10) =>
        new(pageSize) { TotalItems = totalItems };

    [Fact]
    public void Paginate_ReturnsCurrentPageAndRecordsTotal()
    {
        // Arrange
        var state = new PaginationState(10);
        var items = Enumerable.Range(1, 25).ToArray();
        state.TotalItems = items.Length;
        state.GoToPage(3);

        // Act
        var page = state.Paginate(items);

        // Assert
        page.Should().Equal(21, 22, 23, 24, 25);
        state.TotalItems.Should().Be(25);
        state.PageStartIndex.Should().Be(21);
        state.PageEndIndex.Should().Be(25);
    }

    [Fact]
    public void Empty_HasSinglePageAndZeroIndexes()
    {
        var state = WithItems(0);

        state.TotalPages.Should().Be(1);
        state.PageStartIndex.Should().Be(0);
        state.PageEndIndex.Should().Be(0);
        state.HasPreviousPage.Should().BeFalse();
        state.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void FewPages_ShowsAllPagesWithoutEllipsis()
    {
        var state = WithItems(70);

        state.VisiblePageNumbers.Should().Equal(1, 2, 3, 4, 5, 6, 7);
        state.ShowLeadingEllipsis.Should().BeFalse();
        state.ShowTrailingEllipsis.Should().BeFalse();
    }

    [Fact]
    public void ManyPages_ShowsWindowAroundCurrentPage()
    {
        var state = WithItems(200);
        state.GoToPage(10);

        state.VisiblePageNumbers.Should().Equal(8, 9, 10, 11, 12);
        state.ShowLeadingEllipsis.Should().BeTrue();
        state.ShowTrailingEllipsis.Should().BeTrue();
    }

    [Fact]
    public void ManyPages_LastPage_KeepsFullWindow()
    {
        var state = WithItems(200);
        state.GoToPage(20);

        state.VisiblePageNumbers.Should().Equal(16, 17, 18, 19, 20);
        state.ShowTrailingEllipsis.Should().BeFalse();
    }

    [Fact]
    public void GoToPage_OutOfRange_IsIgnored()
    {
        var state = WithItems(30);
        state.GoToPage(2);

        state.GoToPage(4);
        state.GoToPage(0);

        state.CurrentPage.Should().Be(2);
    }

    [Fact]
    public void SetPageSize_ResetsToFirstPage_AndIgnoresNonPositive()
    {
        var state = WithItems(100);
        state.GoToPage(5);

        state.SetPageSize(25);
        state.SetPageSize(0);

        state.PageSize.Should().Be(25);
        state.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void CurrentPage_IsClampedWhenItemsShrink()
    {
        var state = WithItems(100);
        state.GoToPage(10);

        state.TotalItems = 15;

        state.CurrentPage.Should().Be(2);
    }
}
