using Anarchy.Toolkit.Core.Service.Client.Exceptions;
using Anarchy.Toolkit.Tests.Unit.Core.Dummy;
using AwesomeAssertions;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Service.Client.Resolver;

public class KeyedClientResolverTest
{
    private readonly Dictionary<string, IDummyClient> _clientMap;
    private readonly DummyClientResolver _resolver;

    public KeyedClientResolverTest()
    {
        var mockClient1 = new Mock<IDummyClient>();
        mockClient1.Setup(c => c.ClientKey).Returns("distributor1");

        var mockClient2 = new Mock<IDummyClient>();
        mockClient2.Setup(c => c.ClientKey).Returns("distributor2");

        _clientMap = new Dictionary<string, IDummyClient> {
            { "distributor1", mockClient1.Object }, { "distributor2", mockClient2.Object }
        };

        _resolver = new DummyClientResolver(_clientMap.Values);
    }

    [Fact]
    public void GetClient_WithExistingKey_ReturnsClient()
    {
        var result = _resolver.GetClient("distributor1");

        result.Should().Be(_clientMap["distributor1"]);
    }

    public static TheoryData<string?, Exception> ResolverExceptionTestData()
    {
        return new TheoryData<string?, Exception>
        {
            { null, new ArgumentNullException("key") },
            { "", new ClientNotFoundException("") },
            { "nonexistent", new ClientNotFoundException("nonexistent") }
        };
    }

    [Theory]
    [MemberData(nameof(ResolverExceptionTestData))]
    public void GetClient_ThrowException<T>(string? input, T ex) where T : Exception
    {
        _resolver.Invoking(r => r.GetClient(input!))
            .Should()
            .Throw<T>()
            .WithMessage(ex.Message);
    }

    [Fact]
    public void GetClient_WithUnknownKey_ExposesKeyOnException()
    {
        _resolver.Invoking(r => r.GetClient("nonexistent"))
            .Should()
            .Throw<ClientNotFoundException>()
            .Which.Key.Should().Be("nonexistent");
    }

    [Fact]
    public void Constructor_WithDuplicateKeys_ThrowsArgumentException()
    {
        var client = new Mock<IDummyClient>();
        client.Setup(c => c.ClientKey).Returns("duplicate");

        var act = () => new DummyClientResolver([client.Object, client.Object]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullClients_ThrowsArgumentNullException()
    {
        var act = () => new DummyClientResolver(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("clients");
    }
}
