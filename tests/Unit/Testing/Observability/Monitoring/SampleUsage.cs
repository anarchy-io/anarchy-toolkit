using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Serilog;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring;

public class SampleUsage
{
    [Fact]
    public void WhenAWarningIsLogged_ThenTheMessageAppearsInLogsWithAllTheCorrectParameters()
    {
        var expectedObject = new object();
        var emptyCollection = Enumerable.Empty<object>();
        var fullCollection = new List<object>
                             {
                                 1,
                                 "2",
                                 new Dictionary<string, object>
                                 {
                                     { "3", new object[] { 4, DateTime.Now } }
                                 },
                                 new object[] { 5, "6" }
                             };
        var logger = new LoggerConfiguration()
                    .MinimumLevel.Is(LogEventLevel.Verbose)
                    .WriteTo.InMemory(out var sink)
                    .CreateLogger();

        logger.Warning(
            new NotImplementedException("Ooops!"),
            "Important message with {SomeNumber}, {SomeString}, {SomethingElse}, {@SomeObject}, {EmptyCollection}, {SomeCollection}.",
            42ul,
            "i am a string",
            DateTime.Now,
            expectedObject,
            emptyCollection,
            fullCollection
        );

        var message = sink.Should().HaveMessage(
            "Important message with {SomeNumber}, {SomeString}, {SomethingElse}, {@SomeObject}, {EmptyCollection}, {SomeCollection}."
        ).Which;

        using (new AssertionScope())
        {
            message.Level.Should().Be(LogEventLevel.Warning);

            message.Exception.Should().BeOfType<NotImplementedException>()
                   .Which.Message.Should().Be("Ooops!");

            message.Should().HaveNumericProperty<ulong>("SomeNumber")
                   .Which.Should().Be(42ul);

            message.Should().HaveStringProperty("SomeString")
                   .Which.Should().Be("i am a string");

            message.Should().HaveProperty("SomethingElse");

            message.Should().HaveObjectProperty("SomeObject")
                   .Which.Should().BeSameAs(expectedObject);

            message.Should().HaveCollectionProperty("EmptyCollection")
                   .Which.Should().BeEmpty();

            message.Should().HaveCollectionProperty("SomeCollection")
                   .Which.Should().BeEquivalentTo(fullCollection);
        }
    }
}
