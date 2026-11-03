using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Tests.Unit.Core.Helper;

public class TextMaskerTest
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("ab", "ab")]
    [InlineData("abc", "abc")]
    [InlineData("abcd", "abcd")]
    [InlineData("abcde", "ab*de")]
    public void Mask_GivenInput_ReturnsExpected(string? input, string? expected)
    {
        // Act
        var result = TextMasker.Mask(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("ab", "ab")]
    [InlineData("abc", "a*c")]
    [InlineData("abcd", "a**d")]
    [InlineData("abcde", "a***e")]
    public void Mask_GivenInputAndVisibleCharacterCount_ReturnsExpected(string? input, string? expected)
    {
        // Act
        var result = TextMasker.Mask(input, 1);

        // Assert
        Assert.Equal(expected, result);
    }
}
