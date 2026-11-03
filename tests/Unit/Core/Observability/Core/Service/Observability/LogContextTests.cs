using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Tests.Unit.Core.Observability.TestHelpers;
using AwesomeAssertions;
using Serilog;
using Serilog.Core;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.Core.Service.Observability;

public class LogContextTests
{
    [Fact]
    public void PushPropertyWithNullPropertyShouldThrow()
    {
        var sut = new LogContext();

        sut.Invoking(obj => obj.PushProperty(property: null!))
           .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PushPropertySetsPropertyOnLogContext()
    {
        var sut = new LogContext();

        using (sut.PushProperty(new LogContextProperty(name: "Property", value: "Value")))
        {
            _logger.Information("Log event");
        }

        _sink.Events.Should().HaveCount(1);
        var firstEvent = _sink.Events.First();

        firstEvent.Properties.Should().HaveCount(1);
        var property = firstEvent.Properties.First();

        property.Key.Should().Be("Property");
        property.Value.ToString().Should().Be("\"Value\"");
    }

    [Fact]
    public void PushPropertyDisposeRemovesPropertyFromLogContext()
    {
        var sut = new LogContext();

        using (sut.PushProperty(new LogContextProperty(name: "Property", value: "Value")))
        {
        }

        _logger.Information("Log event");

        _sink.Events.Should().HaveCount(1);
        var firstEvent = _sink.Events.First();

        firstEvent.Properties.Should().BeEmpty();
    }

    [Fact]
    public void PushPropertiesWithNullPropertiesShouldThrow()
    {
        var sut = new LogContext();

        sut.Invoking(obj => obj.PushProperties(properties: null!))
           .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PushPropertiesWithNullPropertyShouldThrow()
    {
        var sut = new LogContext();

        sut.Invoking(obj => obj.PushProperties([null!]))
           .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PushPropertiesSetsPropertiesOnLogContext()
    {
        var sut = new LogContext();

        using (sut.PushProperties(
            new LogContextProperty(name: "Property1", value: "Value1"),
            new LogContextProperty(name: "Property2", value: "Value2")
        ))
        {
            _logger.Information("Log event");
        }

        _sink.Events.Should().HaveCount(1);
        var firstEvent = _sink.Events.First();

        firstEvent.Properties.Should().HaveCount(2);

        firstEvent.Properties.Should()
                  .Contain(
                       p => p.Key == "Property1" &&
                            p.Value.ToString() == "\"Value1\""
                   )
                  .And
                  .Contain(
                       p => p.Key == "Property2" &&
                            p.Value.ToString() == "\"Value2\""
                   );
    }

    [Fact]
    public void PushPropertiesDisposeRemovesPropertiesFromLogContext()
    {
        var sut = new LogContext();

        using (sut.PushProperties(
            new LogContextProperty("Property1", "Value1"),
            new LogContextProperty("Property2", "Value2")
        ))
        {
        }

        _logger.Information("Log event");

        _sink.Events.Should().HaveCount(1);
        var firstEvent = _sink.Events.First();

        firstEvent.Properties.Should().BeEmpty();
    }

    public LogContextTests()
    {
        _logger = new LoggerConfiguration()
                 .Enrich.FromLogContext()
                 .WriteTo.Sink(_sink)
                 .CreateLogger();
    }

    private readonly InMemorySink _sink = new InMemorySink();
    private readonly Logger _logger;
}
