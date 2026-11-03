using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;

/// <summary>
/// Presenter for generic JSON rendering. Handles JSON payload formatting and display logic.
/// </summary>
public class JsonRenderPresenter(
    UiRoutingOptions routingOptions,
    IHostEnvironment hostEnvironment) : BasePresenter(routingOptions, hostEnvironment)
{
    private static readonly JsonSerializerOptions PrettyJsonOptions = new() {
        WriteIndented = true
    };

    public string PrettyJson(JsonElement? payload)
    {
        return payload.HasValue ? JsonSerializer.Serialize(payload.Value, PrettyJsonOptions) : "null";
    }

    public string PrettyJson(JsonElement payload)
    {
        return JsonSerializer.Serialize(payload, PrettyJsonOptions);
    }

    public string PrettyJson(object? payload)
    {
        return payload == null ? "null" : JsonSerializer.Serialize(payload, PrettyJsonOptions);
    }

    public static bool IsScalarJsonValue(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.String
            or JsonValueKind.Number
            or JsonValueKind.True
            or JsonValueKind.False
            or JsonValueKind.Null;
    }

    public bool IsBooleanJsonValue(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
    }

    public string GetBooleanCssClass(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.True ? "json-boolean-true" : "json-boolean-false";
    }

    public string GetBooleanDisplayValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.True ? "true" : "false";
    }

    public static string FormatScalarJsonValue(JsonElement value)
    {
        return value.ValueKind switch {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => "null",
            _ => value.ToString()
        };
    }

    public static bool IsArrayOfScalars(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Array
               && value.EnumerateArray()
                   .All(IsScalarJsonValue);
    }

    public static bool IsArrayOfObjects(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Array
               && value.EnumerateArray()
                   .All(x => x.ValueKind == JsonValueKind.Object);
    }

    public static IReadOnlyCollection<string> GetArrayObjectScalarColumns(JsonElement items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return (from item in items.EnumerateArray()
                where item.ValueKind == JsonValueKind.Object
                from property in item.EnumerateObject()
                where IsScalarJsonValue(property.Value) && seen.Add(property.Name)
                select property.Name).ToList();
    }

    public static IReadOnlyCollection<string> GetArrayObjectComplexColumns(JsonElement items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return (from item in items.EnumerateArray()
                where item.ValueKind == JsonValueKind.Object
                from property in item.EnumerateObject()
                where !IsScalarJsonValue(property.Value) && seen.Add(property.Name)
                select property.Name).ToList();
    }

    public static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject()
                     .Where(property => property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            value = property.Value;
            return true;
        }

        value = default;
        return false;
    }
}
