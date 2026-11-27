using System.Text;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Core.Extensions.System;

/// <summary>
/// String manipulation helpers: truncation, case conversion, sanitisation and obfuscation.
/// </summary>
/// <example>
/// <code language="csharp">
/// "OrderCreated".ToSnakeCase();        // "order_created"
/// "OrderCreated".ToKebabCase();        // "order-created"
/// "john.doe@example.com".ObfuscateEmail(); // "jo…e@example.com"
/// "a long description".Left(6, "...");    // "a long..."
/// </code>
/// </example>
public static partial class StringExtensions
{
    /// <summary>
    /// Returns a substring of the input value with a limited character count
    /// </summary>
    /// <param name="value">This extended string</param>
    /// <param name="take">how many characters will be in the output</param>
    /// <param name="suffix">optional suffix to append if the string is truncated</param>
    /// <returns>
    /// reduced string; <see langword="null"/> when <paramref name="value"/> is <see langword="null"/>, and the
    /// original string (without <paramref name="suffix"/>) when it is not longer than <paramref name="take"/>
    /// </returns>
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
        /// <summary>
        /// Converts a PascalCase/camelCase string to lower <c>snake_case</c> by prefixing every upper-case character
        /// (except the first) with an underscore.
        /// </summary>
        /// <returns>The snake_case string.</returns>
        public string ToSnakeCase()
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
        }

        /// <summary>
        /// Converts a PascalCase/camelCase string to lower <c>kebab-case</c> by prefixing every upper-case character
        /// (except the first) with a hyphen.
        /// </summary>
        /// <returns>The kebab-case string.</returns>
        public string ToKebabCase()
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
        }

        /// <summary>
        /// Lower-cases the first character (invariant culture), leaving the rest untouched.
        /// </summary>
        /// <returns>The camelCase string.</returns>
        /// <exception cref="IndexOutOfRangeException">The string is empty.</exception>
        public string ToCamelCase()
        {
            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Upper-cases the first character (invariant culture), leaving the rest untouched.
        /// </summary>
        /// <returns>The PascalCase string.</returns>
        /// <exception cref="IndexOutOfRangeException">The string is empty.</exception>
        public string ToPascalCase()
        {
            return char.ToUpperInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Removes every whitespace character (as defined by <see cref="char.IsWhiteSpace(char)"/>).
        /// </summary>
        /// <returns>The string without whitespace.</returns>
        public string RemoveWhiteSpace()
        {
            return new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// Removes non-ASCII and non-latin characters from input
        /// </summary>
        /// <remarks>
        /// Keeps printable ASCII (U+0020–U+007E) and Latin-1 Supplement (U+00A0–U+00FF) characters; the result is
        /// trimmed.
        /// </remarks>
        /// <returns>sanitized string</returns>
        public string SanitizeNonLatin()
        {
            return SanitizationRegex().Replace(str, string.Empty).Trim();
        }

        /// <summary>
        /// Checks whether the string (typically a queue/topic name) denotes a FIFO resource, i.e. ends with
        /// <c>.fifo</c> (case-insensitive, ignoring surrounding whitespace).
        /// </summary>
        /// <returns><see langword="true"/> when the name ends with <c>.fifo</c>.</returns>
        public bool IsFifo()
        {
            return str.Trim().EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the number of bytes needed to encode the string as UTF-8 (useful for payload size limits).
        /// </summary>
        /// <returns>The UTF-8 byte count.</returns>
        public long Utf8ByteCount()
        {
            return Encoding.UTF8.GetByteCount(str);
        }

        /// <summary>
        /// Splits camelCase/PascalCase words into a sentence, lower-casing every word after the first
        /// (e.g. <c>"OrderWasCreated"</c> → <c>"Order was created"</c>).
        /// </summary>
        /// <returns>The sentence-case string.</returns>
        public string ToSentenceCase()
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
        }

        /// <summary>
        /// Splits camelCase/PascalCase words keeping their capitalisation and upper-cases the first character
        /// (e.g. <c>"orderWasCreated"</c> → <c>"Order Was Created"</c>). The input is trimmed first.
        /// </summary>
        /// <returns>The title-cased sentence.</returns>
        public string ToTitleSentenceCase()
        {
            return Regex.Replace(str.Trim(), "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}")
                .ToUpperCaseFirstChar();
        }

        /// <summary>
        /// Upper-cases the first character using the current culture, leaving the rest untouched.
        /// </summary>
        /// <returns>The string with an upper-case first character.</returns>
        /// <exception cref="IndexOutOfRangeException">The string is empty.</exception>
        public string ToUpperCaseFirstChar()
        {
            return string.Concat(str[0]
                .ToString()
                .ToUpper(), str.AsSpan(1));
        }

        /// <summary>
        /// Prepends <paramref name="leadingChar"/> unless the string already starts with it
        /// (e.g. ensuring a leading <c>/</c> on a path).
        /// </summary>
        /// <param name="leadingChar">The character the result must start with.</param>
        /// <returns>The string, guaranteed to start with <paramref name="leadingChar"/>.</returns>
        public string EnsureLeadingChar(char leadingChar)
        {
            return str.StartsWith(leadingChar) ? str : $"{leadingChar}{str}";
        }

        /// <summary>
        /// Obfuscates an e-mail address for logging, keeping the first two characters and the last character of the
        /// local part plus the domain (e.g. <c>"john.doe@example.com"</c> → <c>"jo…e@example.com"</c>).
        /// </summary>
        /// <remarks>
        /// Strings without <c>@</c>, or whose local part has 2 or fewer characters, are returned unchanged.
        /// </remarks>
        /// <returns>The obfuscated e-mail address.</returns>
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
