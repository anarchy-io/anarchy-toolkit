namespace Anarchy.Toolkit.Core.Helper;

/// <summary>
///     Helps facilitate masking sensitive information in strings.
/// </summary>
public static class TextMasker
{
    /// <summary>
    ///     Returns the first and last <paramref name="visibleCharacterCount"/> characters of the string, masking the
    ///     rest with <c>*</c>.
    /// </summary>
    /// <param name="original">The text to mask; may be <see langword="null"/>.</param>
    /// <param name="visibleCharacterCount">
    ///     How many characters to keep visible at each end. Defaults to 2. Zero or negative masks the whole string.
    /// </param>
    /// <returns>
    ///     The masked text. <paramref name="original"/> is returned unchanged when it is null/whitespace or not longer
    ///     than <c>2 * visibleCharacterCount</c>.
    /// </returns>
    /// <example>
    /// <code language="csharp">
    /// TextMasker.Mask("1234567890");    // "12******90"
    /// TextMasker.Mask("secret", 0);     // "******"
    /// </code>
    /// </example>
    public static string? Mask(string? original, int visibleCharacterCount = 2)
    {
        const char maskChar = '*';

        if (string.IsNullOrWhiteSpace(original))
        {
            return original;
        }

        if (visibleCharacterCount <= 0)
        {
            return new string(maskChar, original.Length);
        }

        if (original.Length <= visibleCharacterCount * 2)
        {
            return original;
        }

        var maskedLength = original.Length - visibleCharacterCount * 2;
        var mask = new string(maskChar, maskedLength);

        return string.Concat(
            original.AsSpan(0, visibleCharacterCount),
            mask,
            original.AsSpan(original.Length - visibleCharacterCount));
    }
}
