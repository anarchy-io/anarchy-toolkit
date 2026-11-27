using Amazon.SQS.Model;
using Anarchy.Toolkit.Bootstrap.Aws.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Aws.Helpers.MessageBroker;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Integration.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

/// <summary>
/// Integration tests for standard AWS SNS/SQS messaging against the local Moto service.
/// Each test demonstrates per-test resource isolation by creating a uniquely named topic and queue and
/// cleaning them up in <c>finally</c>.
/// </summary>
public class SnsMessageBrokerPublisherTest : TestCaseBase
{
    private const string MotoEndpoint = "http://localhost:4566";
    private const string Region = "eu-west-1";
    private const string DefaultAccountId = "000000000000";
    private const string DummyCredential = "test";
    private const string MessageSource = "integration-test";

    [Fact]
    public async Task PublishAsync_WhenStandardTopicHasSubscribedQueue_DeliversRawPayloadAndAttributes()
    {
        var ct = TestContext.Current.CancellationToken;
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var topicName = $"it-sns-topic-{suffix}";
        var queueName = $"it-sqs-queue-{suffix}";
        var correlationId = TestHelper.RandomId("corr");
        var payload = $"{{\"orderId\":\"{suffix}\",\"status\":\"created\"}}";

        using var broker = CreateBrokerHelper();

        try
        {
            await broker.PrepareTopicAsync(topicName, ct);
            await broker.PrepareQueueAsync(queueName, ct);
            await broker.SubscribeSqsQueueToSnsTopicAsync(queueName, topicName, ct);

            var publisher = await CreatePublisherAsync(broker, topicName);
            var outbound = new OutboundMessageBuilder(topicName, MessageSource).Build(
                payload,
                attributes: new Dictionary<string, string> {
                    ["EventType"] = "OrderCreated",
                    ["CorrelationId"] = correlationId
                });

            await publisher.PublishAsync(outbound, ct);

            var messages = await ReceiveMessagesUntilAsync(broker, queueName, expectedCount: 1, ct);

            messages.Should().ContainSingle();
            var received = messages[0];
            received.Body.Should().Be(payload);
            received.MessageAttributes.Should().NotBeNull();
            received.MessageAttributes.Should().ContainKey("Source");
            received.MessageAttributes["Source"].StringValue.Should().Be(MessageSource);
            received.MessageAttributes.Should().ContainKey("EventType");
            received.MessageAttributes["EventType"].StringValue.Should().Be("OrderCreated");
            received.MessageAttributes.Should().ContainKey("CorrelationId");
            received.MessageAttributes["CorrelationId"].StringValue.Should().Be(correlationId);
        }
        finally
        {
            await CleanupResourcesAsync(broker, topicName, queueName);
        }
    }

    [Fact]
    public async Task PublishAsync_WhenPublishingBatchToStandardTopic_DeliversAllMessagesToSubscribedQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var topicName = $"it-sns-batch-topic-{suffix}";
        var queueName = $"it-sqs-batch-queue-{suffix}";

        using var broker = CreateBrokerHelper();

        try
        {
            await broker.PrepareTopicAsync(topicName, ct);
            await broker.PrepareQueueAsync(queueName, ct);
            await broker.SubscribeSqsQueueToSnsTopicAsync(queueName, topicName, ct);

            var publisher = await CreatePublisherAsync(broker, topicName);
            var builder = new OutboundMessageBuilder(topicName, MessageSource);
            var expectedPayloads = new[]
            {
                $"{{\"batch\":\"{suffix}\",\"sequence\":1}}",
                $"{{\"batch\":\"{suffix}\",\"sequence\":2}}",
                $"{{\"batch\":\"{suffix}\",\"sequence\":3}}"
            };

            var batch = expectedPayloads
                .Select((body, index) => builder.Build(
                    body,
                    attributes: new Dictionary<string, string> {
                        ["Sequence"] = (index + 1).ToString()
                    }))
                .ToArray();

            await publisher.PublishAsync(batch, cancellationToken: ct);

            var messages = await ReceiveMessagesUntilAsync(broker, queueName, expectedCount: expectedPayloads.Length, ct);

            messages.Should().HaveCount(expectedPayloads.Length);
            messages.Select(m => m.Body).Should().BeEquivalentTo(expectedPayloads);
            messages.Should().OnlyContain(m =>
                m.MessageAttributes != null
                && m.MessageAttributes.ContainsKey("Source")
                && m.MessageAttributes["Source"].StringValue == MessageSource
                && m.MessageAttributes.ContainsKey("Sequence"));
        }
        finally
        {
            await CleanupResourcesAsync(broker, topicName, queueName);
        }
    }

    private AwsMessageBrokerHelper CreateBrokerHelper()
    {
        return new AwsMessageBrokerHelper(new MotoAwsAppSettings(), FixtureTelemetry.Telemetry);
    }

    private async Task<SnsMessageBrokerPublisher> CreatePublisherAsync(
        AwsMessageBrokerHelper broker,
        string topicName)
    {
        var topic = await broker.ClientSns.FindTopicAsync(topicName);
        topic.Should().NotBeNull();

        var arnParts = topic!.TopicArn.Split(':');
        var accountId = arnParts.Length > 4 && !string.IsNullOrWhiteSpace(arnParts[4])
            ? arnParts[4]
            : DefaultAccountId;

        var requestBuilder = new SnsAwsPublisherRequestBuilder(new AwsMessagingOptions(Region, accountId));
        return new SnsMessageBrokerPublisher(broker.ClientSns, requestBuilder, FixtureTelemetry.Telemetry);
    }

    private static async Task<IReadOnlyList<Message>> ReceiveMessagesUntilAsync(
        AwsMessageBrokerHelper broker,
        string queueName,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        var received = new Dictionary<string, Message>(StringComparer.Ordinal);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (received.Count < expectedCount)
        {
            var response = await broker.ReceiveMessagesAsync(
                queueName,
                waitTimeSeconds: 2,
                cancellationToken: timeout.Token);

            foreach (var message in response.Messages ?? [])
            {
                received.TryAdd(message.MessageId, message);
            }

            if (received.Count < expectedCount)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
            }
        }

        return [.. received.Values];
    }

    private static async Task CleanupResourcesAsync(
        AwsMessageBrokerHelper broker,
        string topicName,
        string queueName)
    {
        try
        {
            if (await broker.QueueExistsAsync(queueName, CancellationToken.None))
            {
                await broker.DeleteQueueAsync(queueName, CancellationToken.None);
            }
        }
        finally
        {
            await broker.DeleteTopicAsync(topicName, CancellationToken.None);
        }
    }

    private sealed record MotoAwsAppSettings : IAwsAppSettings
    {
        public AwsSettings Aws { get; init; } = new() {
            RegionName = Region,
            ApiVersion = "2012-11-05",
            AccountId = DefaultAccountId,
            EndpointSns = MotoEndpoint,
            EndpointSqs = MotoEndpoint,
            AccessKey = DummyCredential,
            SecretKey = DummyCredential
        };
    }
}
