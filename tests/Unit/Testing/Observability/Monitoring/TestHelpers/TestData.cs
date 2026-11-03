namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;

/// <summary>
/// Anonymous test values (replaces the legacy AutoFixture <c>Fixture.Create&lt;T&gt;()</c>).
/// </summary>
internal static class TestData
{
    public static string AnyString() => Guid.NewGuid().ToString();

    public static int AnyInt() => Random.Shared.Next(1, int.MaxValue);

    public static char AnyChar() => (char)Random.Shared.Next('a', 'z' + 1);
}
