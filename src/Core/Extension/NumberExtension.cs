namespace Anarchy.Toolkit.Core.Extension;

public static class NumberExtension
{
    public static bool IsBetween(this int? value, int start, int end)
    {
        return value.HasValue && value.Value >= start && value.Value <= end;
    }
}
