using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Globalization;

namespace Anarchy.Toolkit.Core.Extension;

public static class JsonExtension
{
    public const NullValueHandling DefaultNullValueHandling = NullValueHandling.Include;
    public const Formatting DefaultFormatting = Formatting.None;
    public const DateTimeStyles DefaultDateTimeStyles = DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Serializes any values as JSON
    /// </summary>
    /// <param name="value"></param>
    /// <param name="contractResolver">Optional override to change the default contract resolver</param>
    /// <param name="formatting">Output formatting</param>
    /// <remarks>Enforces the usage of ISO data/time formats on relevant fields of the input value</remarks>
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
    /// Tries to create an instance of <c>TOutput</c> by parsing a JSON string
    /// </summary>
    /// <param name="input">JSON string</param>
    /// <param name="defaultValue">A non-null value will prevent <see cref="EmptyJsonValueException"/></param>
    /// <typeparam name="TOutput">Type that matches the structure of <c>TOutput</c></typeparam>
    /// <returns></returns>
    /// <exception cref="JsonException">When the input is not a valid JSON string</exception>
    /// <exception cref="EmptyJsonValueException">When string is parseable but results in <c>null</c> and no default value was provided</exception>
    public static TOutput FromJson<TOutput>(this string input, TOutput? defaultValue = default)
    {
        // Explicitly using the native serializer instead of the one from Newtonsoft due to its exception handling
        return System.Text.Json.JsonSerializer.Deserialize<TOutput>(input) ?? defaultValue ??
            throw new EmptyJsonValueException(typeof(TOutput));
    }
}
