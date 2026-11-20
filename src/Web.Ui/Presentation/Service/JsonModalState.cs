using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Presentation.Service;

/// <summary>
/// Circuit-scoped state of the JSON modal, with a breadcrumb history of nested views.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddToolkitUiPresentation</c>, so each Blazor circuit has its own modal.
/// Components open the modal with <see cref="Open"/> and the modal host re-renders on <see cref="Changed"/>.
/// Drilling into nested values appends entries to <see cref="History"/>; breadcrumbs navigate back with
/// <see cref="NavigateTo"/>. Not thread-safe; use it from the circuit's synchronization context.
/// </remarks>
/// <example>
/// <code language="csharp">
/// modalState.Open("Order / 42", orderJson);
/// modalState.Open("Order / 42 / Lines", linesJson, appendToHistory: true);
/// modalState.NavigateTo(0); // back to "Order / 42"
/// </code>
/// </example>
public class JsonModalState
{
    /// <summary>The separator between breadcrumb segments in entry titles.</summary>
    public const string BreadcrumbSeparator = " / ";

    private readonly List<JsonModalEntry> _history = [];

    /// <summary>The title of the current entry.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>The JSON content of the current entry.</summary>
    public string JsonContent { get; private set; } = string.Empty;
    /// <summary>Whether the current entry is shown as preformatted text instead of the structured JSON view.</summary>
    public bool UsePreformattedView { get; private set; }
    /// <summary>Whether the modal is open.</summary>
    public bool IsOpen { get; private set; }
    /// <summary>The entries opened since the modal was opened, oldest first; the last one is current.</summary>
    public IReadOnlyList<JsonModalEntry> History => _history;

    /// <summary>Raised whenever the modal state changes.</summary>
    public event Action? Changed;

    /// <summary>
    /// Opens the modal with a new entry, either starting a new history or appending to the current one.
    /// </summary>
    /// <param name="title">The entry title; use <see cref="BreadcrumbSeparator"/> to express nesting.</param>
    /// <param name="jsonContent">The JSON to display.</param>
    /// <param name="usePreformattedView">Whether to show the content as preformatted text.</param>
    /// <param name="appendToHistory">
    /// When <see langword="true"/> and the modal is already open, the entry is appended to the history; otherwise
    /// the history is cleared first.
    /// </param>
    public void Open(
        string title,
        string jsonContent,
        bool usePreformattedView = false,
        bool appendToHistory = false)
    {
        if (!appendToHistory || !IsOpen)
        {
            _history.Clear();
        }

        _history.Add(new JsonModalEntry(title, jsonContent, usePreformattedView));
        ApplyCurrentEntry(_history[^1]);
        IsOpen = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Navigates back to the history entry at <paramref name="historyIndex"/>, discarding the entries after it.
    /// Ignored when the modal is closed or the index is out of range.
    /// </summary>
    /// <param name="historyIndex">The 0-based index in <see cref="History"/>.</param>
    public void NavigateTo(int historyIndex)
    {
        if (!IsOpen || historyIndex < 0 || historyIndex >= _history.Count)
        {
            return;
        }

        _history.RemoveRange(historyIndex + 1, _history.Count - (historyIndex + 1));
        ApplyCurrentEntry(_history[^1]);
        Changed?.Invoke();
    }

    /// <summary>Closes the modal and clears its state and history. Ignored when already closed.</summary>
    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _history.Clear();
        Title = string.Empty;
        JsonContent = string.Empty;
        UsePreformattedView = false;
        Changed?.Invoke();
    }

    private void ApplyCurrentEntry(JsonModalEntry entry)
    {
        Title = entry.Title;
        JsonContent = entry.JsonContent;
        UsePreformattedView = entry.UsePreformattedView;
    }

    /// <summary>
    /// Extracts the ID from JSON content for breadcrumb display.
    /// Looks for properties named "ID" (case-insensitive).
    /// </summary>
    /// <param name="jsonContent">The JSON content.</param>
    /// <returns>
    /// The ID (string value or raw number text), or <see langword="null"/> when the content is not a JSON object,
    /// is invalid JSON, or has no string/number ID property.
    /// </returns>
    public static string? ExtractIdFromJson(string jsonContent)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in root.EnumerateObject()
                         .Where(property => property.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
            {
                return property.Value.ValueKind switch {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return null
        }

        return null;
    }

    /// <summary>
    /// Builds a breadcrumb title from path segments and optional ID.
    /// Example: BuildBreadcrumbTitle(["Product"], "123") => "Product / 123"
    /// </summary>
    /// <param name="pathSegments">The breadcrumb path segments.</param>
    /// <param name="id">An optional ID appended as the last segment when not empty.</param>
    /// <returns>The segments joined with <see cref="BreadcrumbSeparator"/>.</returns>
    public static string BuildBreadcrumbTitle(string[] pathSegments, string? id = null)
    {
        var parts = new List<string>(pathSegments);
        if (!string.IsNullOrEmpty(id))
        {
            parts.Add(id);
        }

        return string.Join(BreadcrumbSeparator, parts);
    }

    /// <summary>
    /// Returns the history index to navigate to when the breadcrumb segment at <paramref name="segmentIndex"/>
    /// is clicked: the latest entry whose title has that many segments.
    /// </summary>
    /// <param name="segmentIndex">The 0-based index of the clicked breadcrumb segment.</param>
    /// <returns>The matching history index, or <c>0</c> when none matches.</returns>
    public int GetHistoryIndexForBreadcrumb(int segmentIndex)
    {
        var targetSegmentCount = segmentIndex + 1;
        for (var i = _history.Count - 1; i >= 0; i--)
        {
            if (_history[i].Title.Split(BreadcrumbSeparator).Length == targetSegmentCount)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>An entry in the JSON modal history.</summary>
    /// <param name="Title">The entry title (breadcrumb).</param>
    /// <param name="JsonContent">The JSON to display.</param>
    /// <param name="UsePreformattedView">Whether to show the content as preformatted text.</param>
    public sealed record JsonModalEntry(string Title, string JsonContent, bool UsePreformattedView);
}
