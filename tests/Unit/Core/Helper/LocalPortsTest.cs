using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using System.Net;
using System.Net.Sockets;

namespace Anarchy.Toolkit.Tests.Unit.Core.Helper;

public class LocalPortsTest : TestCaseBase
{
    [Fact]
    public void Assign_WhenNoPortsAreUsed_AssignsPairsFromFirst()
    {
        var result = LocalPorts.Assign(["a", "b"], []);

        result["a"].Should().Be((5000, 5001));
        result["b"].Should().Be((5002, 5003));
    }

    [Fact]
    public void Assign_WhenPortsAreUsed_SkipsOverlappingPairs()
    {
        var result = LocalPorts.Assign(["a"], [7001], first: 7000);

        result["a"].Should().Be((7002, 7003));
    }

    [Fact]
    public void FindAvailable_WhenPreferredPortIsTaken_ReturnsAnotherFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var taken = ((IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            LocalPorts.IsAvailable(taken).Should().BeFalse();
            var port = LocalPorts.FindAvailable(taken);
            port.Should().NotBe(taken);
            LocalPorts.IsAvailable(port).Should().BeTrue();
        }
        finally
        {
            listener.Stop();
        }
    }
}
