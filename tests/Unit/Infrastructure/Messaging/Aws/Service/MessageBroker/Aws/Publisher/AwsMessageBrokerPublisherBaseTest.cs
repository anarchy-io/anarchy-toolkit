using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

public class AwsMessageBrokerPublisherBaseTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishAsync_ReportsLogsAndMetricsToTheConsumerSink(bool fail)
    {
        var token = TestContext.Current.CancellationToken;
        var sink = new InMemorySink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.AddTelemetryLogMetrics();
        using var provider = services.BuildServiceProvider();
        var listener = provider.GetServices<IHostedService>().Should().ContainSingle().Which;
        await listener.StartAsync(token);
        var publisher = new Publisher(provider.GetRequiredService<IAppTelemetry>(), fail);
        var message = new OutboundMessageEntity("1", "orders", "message", deduplicationId: "1");

        if (fail)
        {
            var action = () => publisher.PublishAsync(message, token);
            await action.Should().ThrowAsync<MessagePublishException>();
        }
        else
        {
            await publisher.PublishAsync(message, token);
        }

        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains(fail ? "Failed to publish" : "Published outbound"));
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("publish_async.execution_count"));
        await listener.StopAsync(token);
    }

    private sealed class Publisher(IAppTelemetry telemetry, bool fail)
        : AwsMessageBrokerPublisherBase<string, string[]>(new Builder(), telemetry)
    {
        protected override Task SendRequestAsync(string request, CancellationToken cancellationToken) =>
            fail ? Task.FromException(new InvalidOperationException("failed")) : Task.CompletedTask;

        protected override Task SendBatchRequestAsync(string[] request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class Builder : IAwsPublisherRequestBuilder<string, string[]>
    {
        public string FromOutboundMessage(OutboundMessageEntity message) => message.Message;
        public string[] FromOutboundMessage(OutboundMessageEntity[] messages) => messages.Select(message => message.Message).ToArray();
    }
}
