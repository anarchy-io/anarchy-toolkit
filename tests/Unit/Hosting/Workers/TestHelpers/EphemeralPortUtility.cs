using System.Net;
using System.Net.Sockets;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;

internal static class EphemeralPortUtility
{
    private const int MinimumEphemeralPortNumber = 1024;
    private const int MaximumEphemeralPortNumber = 65535;

    private static readonly Random Random = new();

    public static int FindFreeEphemeralTcpPort()
    {
        const int maximumNumberOfAttempts = 10;
        const int excludingMaxPortValue = MaximumEphemeralPortNumber + 1;

        for (var attempt = 0; attempt < maximumNumberOfAttempts; attempt++)
        {
            int portNumber;
            lock (Random)
            {
                portNumber = Random.Next(MinimumEphemeralPortNumber, excludingMaxPortValue);
            }

            if (IsTcpPortAvailable(portNumber))
                return portNumber;
        }

        throw new InvalidOperationException(
            $"Unable to find a free TCP port after {maximumNumberOfAttempts} attempts.");
    }

    private static bool IsTcpPortAvailable(int portNumber)
    {
        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Loopback, portNumber);
            listener.Start();

            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            listener?.Stop();
        }
    }
}
