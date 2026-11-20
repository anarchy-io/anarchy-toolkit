using Anarchy.Toolkit.Automation.Build.Infrastructure.Telemetry;
using Anarchy.Toolkit.Core.Telemetry;
using AwesomeAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Build.Infrastructure.Telemetry;

[CollectionDefinition(nameof(StaticSerilogLogger), DisableParallelization = true)]
public sealed class StaticSerilogLogger;

/// <summary>Replaces the static <see cref="Log.Logger"/>, so it runs alone.</summary>
[Collection(nameof(StaticSerilogLogger))]
public class BuildTelemetryAttributeTest
{
    private static readonly CallSite CallSite = new("Build", "Compile");

    [Fact]
    public void OnBuildCreated_SendsTheEngineLogsToTheConsumerSinksToo()
    {
        var engine = new RecordingSink();
        var consumer = new RecordingSink();
        var previous = Log.Logger;
        try
        {
            using var engineLogger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(engine).CreateLogger();
            Log.Logger = engineLogger;
            var attribute = new BuildTelemetryAttribute();

            attribute.Attach(configuration => configuration.WriteTo.Sink(consumer));
            Log.Information("Compiling {Project}", "Orders");
            BuildTelemetryAttribute.Telemetry.LogInformation(CallSite, "Through the toolkit");
            attribute.OnBuildFinished();

            engine.Events.Select(e => e.RenderMessage()).Should().Contain("Compiling \"Orders\"");
            consumer.Events.Select(e => e.RenderMessage()).Should().Contain("Compiling \"Orders\"")
                .And.Contain(message => message.Contains("Through the toolkit"));
            consumer.Events.Should().AllSatisfy(e => e.Properties.Should().ContainKey("ExecutingTarget"));
            consumer.Disposed.Should().BeTrue("the consumer logger is owned and flushed by the bridge");
            BuildTelemetryAttribute.Telemetry.Should().BeSameAs(AppTelemetry.Null);
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void OnBuildCreated_WithoutConsumerSinks_KeepsTheEngineLogger()
    {
        var engine = new RecordingSink();
        var previous = Log.Logger;
        try
        {
            using var engineLogger = new LoggerConfiguration().WriteTo.Sink(engine).CreateLogger();
            Log.Logger = engineLogger;
            var attribute = new BuildTelemetryAttribute();

            attribute.Attach(_ => null);
            Log.Logger.Should().BeSameAs(engineLogger);
            BuildTelemetryAttribute.Telemetry.LogInformation(CallSite, "Through the toolkit");
            attribute.OnBuildFinished();

            engine.Events.Should().ContainSingle().Which.RenderMessage().Should().Contain("Through the toolkit");
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    private sealed class RecordingSink : ILogEventSink, IDisposable
    {
        public List<LogEvent> Events { get; } = [];
        public bool Disposed { get; private set; }
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
        public void Dispose() => Disposed = true;
    }
}
