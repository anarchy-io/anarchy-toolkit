using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using System.Text.Json;

namespace Anarchy.Toolkit.Web.Ui.Presentation.Presenter;

/// <summary>
/// Presenter for generic JSON rendering. Handles JSON payload formatting and display logic.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddToolkitUiPresentation</c> and used by the toolkit JSON components
/// (JSON modal, property rows, array tables) to classify <see cref="JsonElement"/> values and format them.
/// </remarks>
/// <param name="routingOptions">How rendered links are built.</param>
/// <param name="hostEnvironment">The host environment.</param>
public class JsonRenderPresenter(
    UiRoutingOptions routingOptions,
    IHostEnvironment hostEnvironment) : BasePresenter(routingOptions, hostEnvironment)
{
    private static readonly JsonSerializerOptions PrettyJsonOptions = new() {
        WriteIndented = true
    };

    /// <summary>Serializes <paramref name="payload"/> as indented JSON.</summary>
    /// <param name="payload">The JSON value.</param>
    /// <returns>The indented JSON, or <c>null</c> when <paramref name="payload"/> has no value.</returns>
    public string PrettyJson(JsonElement? payload)
    {
        return payload.HasValue ? JsonSerializer.Serialize(payload.Value, PrettyJsonOptions) : "null";
    }

    /// <summary>Serializes <paramref name="payload"/> as indented JSON.</summary>
    /// <param name="payload">The JSON value.</param>
    /// <returns>The indented JSON.</returns>
    public string PrettyJson(JsonElement payload)
    {
        return JsonSerializer.Serialize(payload, PrettyJsonOptions);
    }

    /// <summary>Serializes <paramref name="payload"/> as indented JSON.</summary>
    /// <param name="payload">The object to serialize.</param>
    /// <returns>The indented JSON, or <c>null</c> when <paramref name="payload"/> is <see langword="null"/>.</returns>
    public string PrettyJson(object? payload)
    {
        return payload == null ? "null" : JsonSerializer.Serialize(payload, PrettyJsonOptions);
    }

    /// <summary>Determines whether <paramref name="value"/> is a string, number, boolean or <c>null</c>.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns><see langword="true"/> for scalar values; <see langword="false"/> for objects and arrays.</returns>
    public static bool IsScalarJsonValue(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.String
            or JsonValueKind.Number
            or JsonValueKind.True
            or JsonValueKind.False
            or JsonValueKind.Null;
    }

    /// <summary>Determines whether <paramref name="value"/> is a JSON boolean.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns><see langword="true"/> for <c>true</c> and <c>false</c> values.</returns>
    public bool IsBooleanJsonValue(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
    }

    /// <summary>Returns the CSS class used to style a boolean value.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns><c>json-boolean-true</c> for <c>true</c>; otherwise <c>json-boolean-false</c>.</returns>
    public string GetBooleanCssClass(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.True ? "json-boolean-true" : "json-boolean-false";
    }

    /// <summary>Returns the display text of a boolean value.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns><c>true</c> for <c>true</c>; otherwise <c>false</c>.</returns>
    public string GetBooleanDisplayValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.True ? "true" : "false";
    }

    /// <summary>Formats a scalar value for display.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns>
    /// The unquoted string, <see cref="bool.TrueString"/>/<see cref="bool.FalseString"/> for booleans, <c>null</c> for
    /// null, or the raw text for other kinds.
    /// </returns>
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

    /// <summary>Determines whether <paramref name="value"/> is an array whose items are all scalars.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns><see langword="true"/> for arrays of scalars (including empty arrays).</returns>
    public static bool IsArrayOfScalars(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Array
               && value.EnumerateArray()
                   .All(IsScalarJsonValue);
    }

    /// <summary>Determines whether <paramref name="value"/> is an array whose items are all objects.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns><see langword="true"/> for arrays of objects (including empty arrays).</returns>
    public static bool IsArrayOfObjects(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Array
               && value.EnumerateArray()
                   .All(x => x.ValueKind == JsonValueKind.Object);
    }

    /// <summary>
    /// Returns the distinct names of properties holding scalar values across all objects of an array, in order of
    /// first appearance. Used as the columns of an object table.
    /// </summary>
    /// <param name="items">A JSON array; non-object items are ignored.</param>
    /// <returns>The column names.</returns>
    public static IReadOnlyCollection<string> GetArrayObjectScalarColumns(JsonElement items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return (from item in items.EnumerateArray()
                where item.ValueKind == JsonValueKind.Object
                from property in item.EnumerateObject()
                where IsScalarJsonValue(property.Value) && seen.Add(property.Name)
                select property.Name).ToList();
    }

    /// <summary>
    /// Returns the distinct names of properties holding objects or arrays across all objects of an array, in order
    /// of first appearance.
    /// </summary>
    /// <param name="items">A JSON array; non-object items are ignored.</param>
    /// <returns>The property names.</returns>
    public static IReadOnlyCollection<string> GetArrayObjectComplexColumns(JsonElement items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return (from item in items.EnumerateArray()
                where item.ValueKind == JsonValueKind.Object
                from property in item.EnumerateObject()
                where !IsScalarJsonValue(property.Value) && seen.Add(property.Name)
                select property.Name).ToList();
    }

    /// <summary>Looks up a property of a JSON object by name, ignoring case.</summary>
    /// <param name="element">The JSON object.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">When found, the property value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when the property exists.</returns>
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
