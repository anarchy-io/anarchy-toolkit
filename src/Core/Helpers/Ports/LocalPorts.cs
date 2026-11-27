using System.Net;
using System.Net.Sockets;

namespace Anarchy.Toolkit.Core.Helpers.Ports;

/// <summary>
/// Local TCP ports: assigns HTTP/HTTPS pairs to hosts and finds free ports on the loopback interface.
/// </summary>
public static class LocalPorts
{
    /// <summary>
    /// The default first HTTP port handed out by <see cref="Assign{T}"/>.
    /// </summary>
    public const int First = 5000;

    /// <summary>
    /// Assigns an HTTP port and the next one for HTTPS to each item, in order, from <paramref name="first"/> upwards,
    /// two apart, skipping any pair that overlaps <paramref name="used"/>.
    /// </summary>
    /// <typeparam name="T">The item type (must be usable as a dictionary key).</typeparam>
    /// <param name="items">The items, in the order they should receive ports.</param>
    /// <param name="used">Ports already taken that must not be assigned.</param>
    /// <param name="first">The first HTTP port; defaults to <see cref="First"/>.</param>
    /// <returns>The HTTP and HTTPS port of each item.</returns>
    public static IReadOnlyDictionary<T, (int Http, int Https)> Assign<T>(
        IEnumerable<T> items, IReadOnlyCollection<int> used, int first = First) where T : notnull
    {
        var result = new Dictionary<T, (int Http, int Https)>();
        var next = first;
        foreach (var item in items)
        {
            while (used.Contains(next) || used.Contains(next + 1)) next += 2;
            result[item] = (next, next + 1);
            next += 2;
        }
        return result;
    }

    /// <summary>
    /// Whether nothing is listening on <paramref name="port"/> on the loopback interface.
    /// </summary>
    /// <param name="port">The TCP port.</param>
    /// <returns><see langword="true"/> when the port could be bound.</returns>
    public static bool IsAvailable(int port)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort) return false;
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns <paramref name="preferred"/> when it is free, otherwise the first free port above it within
    /// <paramref name="maxAttempts"/>, otherwise a port assigned by the operating system.
    /// </summary>
    /// <param name="preferred">The port to try first.</param>
    /// <param name="maxAttempts">How many consecutive ports to try.</param>
    /// <param name="used">Ports to skip even when free.</param>
    /// <returns>A free port.</returns>
    public static int FindAvailable(int preferred, int maxAttempts = 100, IReadOnlyCollection<int>? used = null)
    {
        for (var port = preferred; port < preferred + maxAttempts && port <= IPEndPoint.MaxPort; port++)
        {
            if (used?.Contains(port) == true) continue;
            if (IsAvailable(port)) return port;
        }

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var assigned = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return assigned;
    }
}
