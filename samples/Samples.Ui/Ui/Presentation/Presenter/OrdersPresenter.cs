using Anarchy.Toolkit.Samples.Ui.Core.Entity;
using Anarchy.Toolkit.Samples.Ui.Core.Service;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Model;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;

namespace Anarchy.Toolkit.Samples.Ui.Ui.Presentation.Presenter;

/// <summary>
/// State of the orders page: loading, client-side filtering and pagination, and cancellation.
/// </summary>
public sealed class OrdersPresenter(IOrdersApiClient ordersApiClient, NotificationService notifications)
{
    public const string AllStatuses = "All";

    private IReadOnlyCollection<Order> _orders = [];

    public PaginationState Pagination { get; } = new();
    public IReadOnlyCollection<Order> PageOrders { get; private set; } = [];
    public IReadOnlyCollection<string> StatusOptions { get; private set; } = [AllStatuses];

    public bool IsLoading { get; private set; }
    public string SearchText { get; set; } = string.Empty;
    public string SelectedStatus { get; set; } = AllStatuses;

    /// <summary>The order awaiting confirmation; priority orders need the typed confirmation.</summary>
    public Order? PendingCancellation { get; private set; }

    public bool IsConfirmationOpen => PendingCancellation is { IsPriority: false };
    public bool IsTypedConfirmationOpen => PendingCancellation is { IsPriority: true };

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            _orders = await ordersApiClient.GetOrdersAsync();
            StatusOptions = [AllStatuses, .._orders.Select(x => x.Status).Distinct().Order()];
            ApplyFilters();
        }
        catch (Exception ex)
        {
            notifications.Error(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyFilters()
    {
        Pagination.Reset();
        Refresh();
    }

    /// <summary>Recomputes the visible page; call after any pagination change.</summary>
    public void Refresh()
    {
        var filtered = _orders
            .Where(x => SelectedStatus == AllStatuses || x.Status == SelectedStatus)
            .Where(x => string.IsNullOrWhiteSpace(SearchText)
                        || x.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        || x.Customer.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        PageOrders = Pagination.Paginate(filtered);
    }

    public void RequestCancellation(Order order) => PendingCancellation = order;

    public void CloseCancellation() => PendingCancellation = null;

    public async Task ConfirmCancellationAsync()
    {
        if (PendingCancellation is not { } order)
        {
            return;
        }

        PendingCancellation = null;

        try
        {
            await ordersApiClient.CancelOrderAsync(order.Id);
            notifications.Success($"Order {order.Id} cancelled.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            // A refused cancellation (e.g. a shipped order) shows the API's problem details under "More details".
            notifications.Error(ex);
        }
    }
}
