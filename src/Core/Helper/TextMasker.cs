namespace Anarchy.Toolkit.Core.Helper;

/// <summary>
///     Helps facilitate masking sensitive information in strings.
/// </summary>
public static class TextMasker
{
    /// <summary>
    ///     Returns the first and last number characters of the string, masking the rest.
    /// </summary>
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
