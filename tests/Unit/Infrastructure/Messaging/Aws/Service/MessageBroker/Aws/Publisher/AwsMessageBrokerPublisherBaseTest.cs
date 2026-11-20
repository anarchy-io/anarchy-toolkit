using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

public class AwsMessageBrokerPublisherBaseTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishAsync_ReportsLogsAndMetricsThroughTheInjectedTelemetry(bool fail)
    {
        var token = TestContext.Current.CancellationToken;
        using var telemetry = new TestTelemetry();
        var publisher = new Publisher(telemetry.Telemetry, fail);
        var message = new OutboundMessage("1", "orders", "message", deduplicationId: "1");

        if (fail)
        {
            var action = () => publisher.PublishAsync(message, token);
            await action.Should().ThrowAsync<MessagePublishException>();
        }
        else
        {
            await publisher.PublishAsync(message, token);
        }

        telemetry.Logs.Should().Contain(log => log.Message.Contains(fail ? "Failed to publish" : "Published outbound"));
        telemetry.Metrics.Metrics.Should().Contain(metric => metric.Kind == MetricKind.Counter
                                                            && metric.Name.EndsWith("publish_async.execution_count")
                                                            && metric.HasTag("success", !fail));
    }

    private sealed class Publisher(AppTelemetry telemetry, bool fail)
        : AwsMessageBrokerPublisherBase<string, string[]>(new Builder(), telemetry)
    {
        protected override Task SendRequestAsync(string request, CancellationToken cancellationToken) =>
            fail ? Task.FromException(new InvalidOperationException("failed")) : Task.CompletedTask;

        protected override Task SendBatchRequestAsync(string[] request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class Builder : IAwsPublisherRequestBuilder<string, string[]>
    {
        public string FromOutboundMessage(OutboundMessage message) => message.Message;
        public string[] FromOutboundMessage(OutboundMessage[] messages) => messages.Select(message => message.Message).ToArray();
    }
}
