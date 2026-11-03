using System.Text;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Core.Extension;

public static partial class StringExtension
{
    /// <summary>
    /// Returns a substring of the input value with a limited character count
    /// </summary>
    /// <param name="value">This extended string</param>
    /// <param name="take">how many characters will be in the output</param>
    /// <param name="suffix">optional suffix to append if the string is truncated</param>
    /// <returns>reduced string</returns>
    public static string? Left(this string? value, int take, string? suffix = null)
    {
        return value switch {
            null => value,
            "" => string.Empty,
            _ => take >= value.Length ? value : value[..take] + (suffix ?? string.Empty)
        };
    }

    /// <param name="str">This extended string</param>
    extension(string str)
    {
        public string ToSnakeCase()
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
        }

        public string ToKebabCase()
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        }

        public string ToCamelCase()
        {
            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public string ToPascalCase()
        {
            return char.ToUpperInvariant(str[0]) + str.Substring(1);
        }

        public string RemoveWhiteSpace()
        {
            return new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// Removes non-ASCII and non-latin characters from input
        /// </summary>
        /// <returns>sanitized string</returns>
        public string SanitizeNonLatin()
        {
            return SanitizationRegex().Replace(str, string.Empty).Trim();
        }

        public bool IsFifo()
        {
            return str.Trim().EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        }

        public long Utf8ByteCount()
        {
            return Encoding.UTF8.GetByteCount(str);
        }

        public string ToSentenceCase()
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
        }

        public string ToTitleSentenceCase()
        {
            return Regex.Replace(str.Trim(), "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}")
                .ToUpperCaseFirstChar();
        }

        public string ToUpperCaseFirstChar()
        {
            return string.Concat(str[0]
                .ToString()
                .ToUpper(), str.AsSpan(1));
        }

        public string EnsureLeadingChar(char leadingChar)
        {
            return str.StartsWith(leadingChar) ? str : $"{leadingChar}{str}";
        }

        public string ObfuscateEmail()
        {
            var atSymbolIndex = str.IndexOf('@');
            if (atSymbolIndex <= 2) return str;

            var lastCharBeforeAtSymbol = str[atSymbolIndex - 1];
            var domain = str[atSymbolIndex..];

            return $"{str.Left(2)}…{lastCharBeforeAtSymbol}{domain}";
        }
    }

    [GeneratedRegex(@"[^\u0020-\u007e\u00a0-\u00ff]+")]
    private static partial Regex SanitizationRegex();
}
