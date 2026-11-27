using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Extensions.Serilog.Configuration;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Telemetry.Serilog.Enrichers;

public class CorrelationIdEnricherTest
{
    [Fact]
    public async Task WithCorrelationId_UsesTheMicrosoftLoggingScopeWithoutDuplicatingProperties()
    {
        var sink = new CollectingSink();
        using var serilogLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .WriteTo.Sink(sink)
            .CreateLogger();
        using var loggerFactory = LoggerFactory.Create(builder => {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddSerilog(serilogLogger, dispose: false);
        });
        var logger = loggerFactory.CreateLogger<CorrelationIdEnricherTest>();

        using (CorrelationContext.BeginScope(logger, "correlation-value", "attempt-value"))
        {
            await Task.Yield();
            logger.LogInformation("Message handled");
        }

        var logEvent = sink.Events.Should().ContainSingle().Which;
        logEvent.Properties["CorrelationId"].Should().Be(new ScalarValue("correlation-value"));
        logEvent.Properties["AttemptId"].Should().Be(new ScalarValue("attempt-value"));
    }

    [Fact]
    public void WithCorrelationId_UsesConfiguredNamesAndPreservesExistingProperties()
    {
        CorrelationContext.SetCorrelationValues(null, null);
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId("RequestId", "RequestAttempt")
            .WriteTo.Sink(sink)
            .CreateLogger();

        using (CorrelationContext.BeginScope(NullLogger.Instance, "ambient-correlation", "ambient-attempt",
                   "RequestId", "RequestAttempt"))
        using (global::Serilog.Context.LogContext.PushProperty("RequestId", "existing-correlation"))
        {
            logger.Information("Message handled");
        }

        var logEvent = sink.Events.Should().ContainSingle().Which;
        logEvent.Properties["RequestId"].Should().Be(new ScalarValue("existing-correlation"));
        logEvent.Properties["RequestAttempt"].Should().Be(new ScalarValue("ambient-attempt"));
        logEvent.Properties.Should().NotContainKey("CorrelationId").And.NotContainKey("AttemptId");
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
