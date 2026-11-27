using Anarchy.Toolkit.Core.Exceptions;
using System.ComponentModel;

namespace Anarchy.Toolkit.Core.Extensions.System;

/// <summary>
/// Converts enum values to and from the text of their <see cref="DescriptionAttribute"/>.
/// </summary>
/// <remarks>
/// Values without a <see cref="DescriptionAttribute"/> are ignored when parsing and cause
/// <see cref="MissingEnumDescriptionException"/> when formatting. See also
/// <see cref="Serialization.Json.DescriptionEnumJsonConverter{TEnum}"/> for JSON support.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public enum Status
/// {
///     [Description("active")] Active,
///     [Description("inactive")] Inactive
/// }
///
/// var text = Status.Active.ToDescription();          // "active"
/// var status = "inactive".ToEnumValue&lt;Status&gt;(); // Status.Inactive
/// </code>
/// </example>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the descriptions of all values of <typeparamref name="T"/> that declare a <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>The descriptions, in declaration order.</returns>
    public static IReadOnlyCollection<string> GetAllDescriptions<T>() where T : Enum
    {
        return [.. GetAllEnumDescriptions<T>()];
    }

    /// <param name="enumDescription">The description text to convert.</param>
    extension(string enumDescription)
    {
        /// <summary>
        /// Tries to convert the description into a value of <typeparamref name="T"/> (case-sensitive), without
        /// throwing.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="result">The parsed value, or <see langword="default"/> on failure.</param>
        /// <returns><see langword="true"/> when the conversion succeeded.</returns>
        public bool TryToEnumValue<T>(out T? result) where T : Enum
        {
            result = default;

            try
            {
                result = ToEnumValue<T>(enumDescription);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts the description into a value of <typeparamref name="T"/>, falling back to
        /// <paramref name="defaultValue"/> on any failure.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="defaultValue">The value returned when the description cannot be converted.</param>
        /// <param name="caseSensitiveSearch">Whether the description must match case-sensitively.</param>
        /// <returns>The matching enum value or <paramref name="defaultValue"/>.</returns>
        public T ToEnumValue<T>(T defaultValue, bool caseSensitiveSearch = true)
            where T : Enum
        {
            try
            {
                return ToEnumValue<T>(enumDescription, caseSensitiveSearch);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts the description into the value of <typeparamref name="T"/> whose
        /// <see cref="DescriptionAttribute"/> matches it.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="caseSensitiveSearch">Whether the description must match case-sensitively.</param>
        /// <returns>The matching enum value.</returns>
        /// <exception cref="EmptyArgumentException">The description is null or empty.</exception>
        /// <exception cref="InvalidEnumDescriptionException">No value of <typeparamref name="T"/> has the description.</exception>
        public T ToEnumValue<T>(bool caseSensitiveSearch = true) where T : Enum
        {
            if (enumDescription.IsNullOrEmpty())
            {
                throw new EmptyArgumentException(nameof(enumDescription));
            }

            if (!IsValidNameToConvert<T>(enumDescription, caseSensitiveSearch))
            {
                throw new InvalidEnumDescriptionException(nameof(enumDescription), enumDescription, typeof(T));
            }

            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                if (GetDescriptionOrNull(enumValue)?.ToUpper() == enumDescription.ToUpper())
                {
                    return (T)Enum.Parse(typeof(T), enumValue.ToString());
                }
            }

            throw new InvalidEnumDescriptionException(nameof(enumDescription), enumDescription, typeof(T));
        }
    }

    /// <summary>
    /// Gets the description of an enum value without throwing.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="valueToConvert">The enum value.</param>
    /// <param name="enumDescription">The description, or <paramref name="defaultDescription"/> on failure.</param>
    /// <param name="defaultDescription">The fallback description. Defaults to <c>"Unknown"</c>.</param>
    public static void TryDescription<T>(this T valueToConvert, out string enumDescription,
                                         string defaultDescription = "Unknown") where T : Enum
    {
        try
        {
            enumDescription = valueToConvert.ToDescription();
        }
        catch
        {
            enumDescription = defaultDescription;
        }
    }

    /// <summary>
    /// Gets the text of the <see cref="DescriptionAttribute"/> applied to an enum value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="valueToConvert">The enum value.</param>
    /// <returns>The description text.</returns>
    /// <exception cref="MissingEnumDescriptionException">The value has no <see cref="DescriptionAttribute"/>.</exception>
    /// <exception cref="UndefinedEnumValueException">The value is not defined in <typeparamref name="T"/>.</exception>
    public static string ToDescription<T>(this T valueToConvert) where T : Enum
    {
        foreach (T enumValue in Enum.GetValues(typeof(T)))
        {
            if (Equals(enumValue, valueToConvert))
            {
                return GetDescriptionOrNull(enumValue) ??
                       throw new MissingEnumDescriptionException(valueToConvert);
            }
        }

        throw new UndefinedEnumValueException(nameof(valueToConvert), valueToConvert);
    }

    private static string? GetDescriptionOrNull<T>(T value) where T : Enum
    {
        var fieldInfo = value.GetType().GetField(value.ToString());

        if (fieldInfo != null &&
            Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        return null;
    }

    private static bool IsValidNameToConvert<T>(string nameToConvert, bool caseSensitiveSearch = true) where T : Enum
    {
        var descriptions = GetAllEnumDescriptions<T>();

        return caseSensitiveSearch ?
            descriptions.Any(n => n == nameToConvert) :
            descriptions.Select(x => x.ToUpper()).Any(n => n == nameToConvert.ToUpper());
    }

    private static IEnumerable<string> GetAllEnumDescriptions<T>() where T : Enum
    {
        var descriptions = new List<string>();

        foreach (T enumValue in Enum.GetValues(typeof(T)))
        {
            var description = GetDescriptionOrNull(enumValue);
            if (description != null)
            {
                descriptions.Add(description);
            }
        }

        return descriptions.ToArray();
    }
}
