using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;

/// <summary>
/// Circuit-scoped state of the JSON modal, with a breadcrumb history of nested views.
/// </summary>
public class JsonModalState
{
    public const string BreadcrumbSeparator = " / ";

    private readonly List<JsonModalEntry> _history = [];

    public string Title { get; private set; } = string.Empty;
    public string JsonContent { get; private set; } = string.Empty;
    public bool UsePreformattedView { get; private set; }
    public bool IsOpen { get; private set; }
    public IReadOnlyList<JsonModalEntry> History => _history;

    public event Action? Changed;

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
    /// Example: BuildBreadcrumbTitle("Product", "123") => "Product / 123"
    /// </summary>
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

    public sealed record JsonModalEntry(string Title, string JsonContent, bool UsePreformattedView);
}
