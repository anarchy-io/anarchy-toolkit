using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension.Exceptions;
using System.ComponentModel;

namespace Anarchy.Toolkit.Core.Extension;

public static class EnumExtension
{
    public static IReadOnlyCollection<string> GetAllDescriptions<T>() where T : Enum
    {
        return [.. GetAllEnumDescriptions<T>()];
    }

    extension(string enumDescription)
    {
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
