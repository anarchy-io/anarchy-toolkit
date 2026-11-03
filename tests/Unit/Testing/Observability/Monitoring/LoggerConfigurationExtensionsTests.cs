using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using AwesomeAssertions;
using Serilog;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring;

public class LoggerConfigurationExtensionsTests
{
    [Fact]
    public void InMemoryWithOutSink_SetsInMemorySinkOnLogger()
    {
        var messageTemplate = Guid.NewGuid().ToString();
        var logger = new LoggerConfiguration()
                    .WriteTo.InMemory(out var sink)
                    .CreateLogger();

        logger.Information(messageTemplate);

        sink.Last!.MessageTemplate.Text.Should().Be(messageTemplate);
    }

    [Fact]
    public void InMemoryWithOutSink_SetsObjectTestDestructuringPolicyOnLogger()
    {
        var expectedObject = new object();
        var logger = new LoggerConfiguration()
                    .WriteTo.InMemory(out var sink)
                    .CreateLogger();

        logger.Information("{@Object}", expectedObject);

        sink.Last!
            .Properties.Should().ContainSingle()
            .Which.Value.Should().BeOfType<ScalarValue>()
            .Which.Value.Should().BeSameAs(expectedObject);
    }
}
