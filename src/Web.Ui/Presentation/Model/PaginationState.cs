namespace Anarchy.Toolkit.Web.Ui.Presentation.Model;

/// <summary>
/// Client-side pagination of an in-memory list: current page, page size and the window of page numbers to show.
/// </summary>
/// <remarks>
/// Call <see cref="Paginate{T}"/> on each render to get the current page and refresh <see cref="TotalItems"/>.
/// <see cref="CurrentPage"/> is always clamped to <c>[1, TotalPages]</c>, so shrinking the list never leaves an
/// invalid page selected. With more than 7 pages, a window of 5 page numbers around the current page is shown,
/// with ellipses signalled by <see cref="ShowLeadingEllipsis"/> and <see cref="ShowTrailingEllipsis"/>. Not thread-safe.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var pagination = new PaginationState(pageSize: 25);
/// var pageItems = pagination.Paginate(allItems);
/// pagination.GoToNextPage();
/// </code>
/// </example>
/// <param name="pageSize">The initial page size; non-positive values fall back to <c>10</c>.</param>
public sealed class PaginationState(int pageSize = 10)
{
    /// <summary>The page sizes offered by default by the page size selector: 10, 25, 50 and 100.</summary>
    public static readonly IReadOnlyList<int> DefaultPageSizeOptions = [10, 25, 50, 100];

    private const int MaxPagesWithoutWindow = 7;
    private const int WindowSize = 5;

    private int _currentPage = 1;

    /// <summary>The number of items per page. Change it with <see cref="SetPageSize"/>.</summary>
    public int PageSize { get; private set; } = pageSize > 0 ? pageSize : 10;

    /// <summary>
    /// Total number of items being paginated. Updated by <see cref="Paginate{T}"/>.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>The number of pages; at least <c>1</c>, even when there are no items.</summary>
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));
    /// <summary>The 1-based current page, clamped to <c>[1, TotalPages]</c>.</summary>
    public int CurrentPage => Math.Clamp(_currentPage, 1, TotalPages);
    /// <summary>The 1-based index of the first item on the current page, or <c>0</c> when there are no items.</summary>
    public int PageStartIndex => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    /// <summary>The 1-based index of the last item on the current page.</summary>
    public int PageEndIndex => Math.Min(CurrentPage * PageSize, TotalItems);
    /// <summary>Whether there is a page before the current one.</summary>
    public bool HasPreviousPage => CurrentPage > 1;
    /// <summary>Whether there is a page after the current one.</summary>
    public bool HasNextPage => CurrentPage < TotalPages;
    /// <summary>
    /// The page numbers to render: all pages when there are at most 7, otherwise a window of 5 around the current page.
    /// </summary>
    public IReadOnlyCollection<int> VisiblePageNumbers => BuildVisiblePageNumbers();
    /// <summary>Whether an ellipsis should be shown between page 1 and the visible window.</summary>
    public bool ShowLeadingEllipsis => VisiblePageNumbers.Count > 0 && VisiblePageNumbers.Min() > 2;
    /// <summary>Whether an ellipsis should be shown between the visible window and the last page.</summary>
    public bool ShowTrailingEllipsis => VisiblePageNumbers.Count > 0 && VisiblePageNumbers.Max() < TotalPages - 1;

    /// <summary>
    /// Returns the items of the current page and records the total item count.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">All items to paginate.</param>
    /// <returns>The items of the current page.</returns>
    public IReadOnlyCollection<T> Paginate<T>(IReadOnlyCollection<T> items)
    {
        TotalItems = items.Count;

        return items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
    }

    /// <summary>Returns to the first page (e.g. after the filter changes).</summary>
    public void Reset()
    {
        _currentPage = 1;
    }

    /// <summary>Changes the page size and returns to the first page. Non-positive values are ignored.</summary>
    /// <param name="selectedPageSize">The new page size.</param>
    public void SetPageSize(int selectedPageSize)
    {
        if (selectedPageSize <= 0) return;

        PageSize = selectedPageSize;
        _currentPage = 1;
    }

    /// <summary>Moves to the previous page, if any.</summary>
    public void GoToPreviousPage()
    {
        if (HasPreviousPage)
        {
            _currentPage = CurrentPage - 1;
        }
    }

    /// <summary>Moves to the next page, if any.</summary>
    public void GoToNextPage()
    {
        if (HasNextPage)
        {
            _currentPage = CurrentPage + 1;
        }
    }

    /// <summary>Moves to the given page. Out-of-range page numbers are ignored.</summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    public void GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > TotalPages)
        {
            return;
        }

        _currentPage = pageNumber;
    }

    private IReadOnlyCollection<int> BuildVisiblePageNumbers()
    {
        if (TotalPages <= MaxPagesWithoutWindow)
        {
            return Enumerable.Range(1, TotalPages)
                .ToArray();
        }

        const int halfWindow = WindowSize / 2;
        var startPage = Math.Max(1, CurrentPage - halfWindow);
        var endPage = Math.Min(TotalPages, startPage + WindowSize - 1);

        if (endPage - startPage + 1 < WindowSize)
        {
            startPage = Math.Max(1, endPage - WindowSize + 1);
        }

        return Enumerable.Range(startPage, endPage - startPage + 1)
            .ToArray();
    }
}
