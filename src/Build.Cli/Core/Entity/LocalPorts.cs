namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// Local Kestrel ports: each web host gets an HTTP port from 5000 upwards, two apart, and the next one for HTTPS.
/// </summary>
public static class LocalPorts
{
    /// <summary>
    /// The HTTP port of the first web host; also the default of <c>RunWebHost</c> when no ports are given.
    /// </summary>
    public const int First = 5000;

    /// <summary>
    /// Assigns ports to the web hosts among <paramref name="applications"/>, in order, skipping any pair that
    /// overlaps <paramref name="used"/>. Applications that are not web hosts get no entry.
    /// </summary>
    public static IReadOnlyDictionary<ApplicationSpec, (int Http, int Https)> Assign(
        IEnumerable<ApplicationSpec> applications, IReadOnlyCollection<int> used)
    {
        var result = new Dictionary<ApplicationSpec, (int Http, int Https)>();
        var next = First;
        foreach (var app in applications.Where(app => app.IsWebHost))
        {
            while (used.Contains(next) || used.Contains(next + 1)) next += 2;
            result[app] = (next, next + 1);
            next += 2;
        }
        return result;
    }
}
