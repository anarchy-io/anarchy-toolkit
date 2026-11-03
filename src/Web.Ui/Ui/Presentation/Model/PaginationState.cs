namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Model;

/// <summary>
/// Client-side pagination of an in-memory list: current page, page size and the window of page numbers to show.
/// </summary>
public sealed class PaginationState(int pageSize = 10)
{
    public static readonly IReadOnlyList<int> DefaultPageSizeOptions = [10, 25, 50, 100];

    private const int MaxPagesWithoutWindow = 7;
    private const int WindowSize = 5;

    private int _currentPage = 1;

    public int PageSize { get; private set; } = pageSize > 0 ? pageSize : 10;

    /// <summary>
    /// Total number of items being paginated. Updated by <see cref="Paginate{T}"/>.
    /// </summary>
    public int TotalItems { get; set; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalItems / PageSize));
    public int CurrentPage => Math.Clamp(_currentPage, 1, TotalPages);
    public int PageStartIndex => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int PageEndIndex => Math.Min(CurrentPage * PageSize, TotalItems);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public IReadOnlyCollection<int> VisiblePageNumbers => BuildVisiblePageNumbers();
    public bool ShowLeadingEllipsis => VisiblePageNumbers.Count > 0 && VisiblePageNumbers.Min() > 2;
    public bool ShowTrailingEllipsis => VisiblePageNumbers.Count > 0 && VisiblePageNumbers.Max() < TotalPages - 1;

    /// <summary>
    /// Returns the items of the current page and records the total item count.
    /// </summary>
    public IReadOnlyCollection<T> Paginate<T>(IReadOnlyCollection<T> items)
    {
        TotalItems = items.Count;

        return items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToArray();
    }

    public void Reset()
    {
        _currentPage = 1;
    }

    public void SetPageSize(int selectedPageSize)
    {
        if (selectedPageSize <= 0) return;

        PageSize = selectedPageSize;
        _currentPage = 1;
    }

    public void GoToPreviousPage()
    {
        if (HasPreviousPage)
        {
            _currentPage = CurrentPage - 1;
        }
    }

    public void GoToNextPage()
    {
        if (HasNextPage)
        {
            _currentPage = CurrentPage + 1;
        }
    }

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
