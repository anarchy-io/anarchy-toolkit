using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Globalization;

namespace Anarchy.Toolkit.Core.Extension;

/// <summary>
/// JSON serialization shortcuts used across the toolkit.
/// </summary>
/// <remarks>
/// Serialization (<see cref="ToJson"/>) uses Newtonsoft.Json with ISO-8601 UTC dates
/// (<see cref="DateHelper.GetIsoDateTimeFormat"/>), whereas deserialization (<see cref="FromJson{TOutput}"/>) uses
/// <see cref="System.Text.Json.JsonSerializer"/> with its default options.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var json = order.ToJson();
/// var copy = json.FromJson&lt;Order&gt;();
/// </code>
/// </example>
public static class JsonExtension
{
    /// <summary>Null handling used by <see cref="ToJson"/>: <see cref="NullValueHandling.Include"/>.</summary>
    public const NullValueHandling DefaultNullValueHandling = NullValueHandling.Include;

    /// <summary>Default output formatting used by <see cref="ToJson"/>: <see cref="Formatting.None"/> (compact).</summary>
    public const Formatting DefaultFormatting = Formatting.None;

    /// <summary>
    /// Date/time styles used by the ISO date converter in <see cref="ToJson"/>:
    /// <see cref="DateTimeStyles.AdjustToUniversal"/>.
    /// </summary>
    public const DateTimeStyles DefaultDateTimeStyles = DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Serializes any values as JSON
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="contractResolver">
    /// Optional override to change the default contract resolver (defaults to <see cref="DefaultContractResolver"/>,
    /// i.e. property names as declared)
    /// </param>
    /// <param name="formatting">Output formatting</param>
    /// <remarks>
    /// Enforces the usage of ISO data/time formats on relevant fields of the input value. Null values are included.
    /// </remarks>
    /// <returns>A valid JSON string</returns>
    public static string ToJson(this object value, IContractResolver? contractResolver = null,
                                Formatting formatting = DefaultFormatting)
    {
        var settings = new JsonSerializerSettings {
            ContractResolver = contractResolver ?? new DefaultContractResolver(),
            NullValueHandling = DefaultNullValueHandling,
            Formatting = formatting
        };

        settings.Converters.Add(new IsoDateTimeConverter {
            DateTimeFormat = DateHelper.GetIsoDateTimeFormat,
            DateTimeStyles = DefaultDateTimeStyles
        });

        return JsonConvert.SerializeObject(value, settings);
    }

    /// <summary>
    /// Tries to create an instance of <typeparamref name="TOutput"/> by parsing a JSON string
    /// </summary>
    /// <param name="input">JSON string</param>
    /// <param name="defaultValue">A non-null value will prevent <see cref="EmptyJsonValueException"/></param>
    /// <typeparam name="TOutput">Type that matches the structure of the JSON input</typeparam>
    /// <returns>The deserialized value, or <paramref name="defaultValue"/> when the JSON evaluates to <c>null</c>.</returns>
    /// <exception cref="System.Text.Json.JsonException">When the input is not a valid JSON string</exception>
    /// <exception cref="EmptyJsonValueException">When string is parseable but results in <c>null</c> and no default value was provided</exception>
    public static TOutput FromJson<TOutput>(this string input, TOutput? defaultValue = default)
    {
        // Explicitly using the native serializer instead of the one from Newtonsoft due to its exception handling
        return System.Text.Json.JsonSerializer.Deserialize<TOutput>(input) ?? defaultValue ??
            throw new EmptyJsonValueException(typeof(TOutput));
    }
}
