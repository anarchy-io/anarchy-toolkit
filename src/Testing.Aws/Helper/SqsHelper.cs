using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Testing.Helper;

namespace Anarchy.Toolkit.Testing.Aws.Helper;

/// <summary>
/// Lightweight SQS helper for building <see cref="SQSEvent"/> instances and for
/// receiving/purging/creating queues (typically against LocalStack).
/// </summary>
/// <typeparam name="TAppSettings">
/// The consumer's concrete application settings type, whose test settings provide the SQS endpoint.
/// </typeparam>
/// <remarks>
/// The client uses dummy <c>test</c>/<c>test</c> credentials, region <c>eu-west-1</c>, and the
/// <c>Aws.EndpointSqs</c> setting from <see cref="Anarchy.Toolkit.Testing.Helper.TestHelper.TestSettings{TAppSettings}"/>.
/// Queue URLs are cached in a static, non-thread-safe dictionary per <typeparamref name="TAppSettings"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// await SqsHelper&lt;AppSettings&gt;.PurgeQueue("orders-queue");
/// // ... run code that publishes to the queue ...
/// var received = await SqsHelper&lt;AppSettings&gt;.ReceiveMessagesFromQueue("orders-queue");
/// var sqsEvent = SqsHelper&lt;AppSettings&gt;.CreateSqsEvent(received.Messages);
/// </code>
/// </example>
public class SqsHelper<TAppSettings>
    where TAppSettings : class, IAwsAppSettings
{
    /// <summary>
    /// The shared SQS client, pointed at the configured test SQS endpoint (e.g. LocalStack).
    /// </summary>
    public static readonly AmazonSQSClient SqsClient = new(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig {
            ServiceURL = TestHelper.TestSettings<TAppSettings>().Aws.EndpointSqs,
            AuthenticationRegion = "eu-west-1"
        });

    private static readonly Dictionary<string, string> QueueUrls = new();

    /// <summary>
    /// Builds an <see cref="SQSEvent"/> from a collection of SQS <see cref="Message"/> instances.
    /// </summary>
    /// <param name="messages">The source SQS messages.</param>
    /// <returns>The constructed <see cref="SQSEvent"/>.</returns>
    /// <remarks>
    /// Useful to feed messages received from a real queue into a Lambda SQS handler under test. The event source ARN is
    /// the placeholder <c>test-queue-arn</c> and the region <c>eu-west-1</c>.
    /// </remarks>
    public static SQSEvent CreateSqsEvent(IEnumerable<Message> messages)
    {
        return new SQSEvent {
            Records = messages.Select(m => new SQSEvent.SQSMessage {
                MessageId = m.MessageId,
                ReceiptHandle = m.ReceiptHandle,
                Body = m.Body,
                Attributes = m.Attributes,
                EventSource = "aws:sqs",
                EventSourceArn = "test-queue-arn",
                AwsRegion = "eu-west-1",
                MessageAttributes = m.MessageAttributes.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new SQSEvent.MessageAttribute {
                            StringValue = kvp.Value.StringValue,
                            BinaryValue = kvp.Value.BinaryValue,
                            DataType = kvp.Value.DataType,
                            StringListValues = kvp.Value.StringListValues,
                            BinaryListValues = kvp.Value.BinaryListValues
                        })
            })
                .ToList()
        };
    }

    /// <summary>
    /// Receives up to 10 messages from the named queue, creating it first if unknown.
    /// </summary>
    /// <param name="queueName">The SQS queue name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The receive response, including all message and system attributes. Long polling waits up to 1 second.</returns>
    public static async Task<ReceiveMessageResponse> ReceiveMessagesFromQueue(
        string queueName, CancellationToken cancellationToken = default)
    {
        if (!QueueUrls.TryGetValue(queueName, out var queueUrl))
        {
            queueUrl = await CreateQueue(queueName, cancellationToken);
        }

        return await SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 1,
            MessageAttributeNames = ["All"],
            MessageSystemAttributeNames = ["All"]
        }, cancellationToken);
    }

    /// <summary>
    /// Purges all messages from the named queue, creating it first if unknown.
    /// </summary>
    /// <param name="queueName">The SQS queue name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the purge request has been accepted.</returns>
    public static async Task PurgeQueue(string queueName, CancellationToken cancellationToken = default)
    {
        if (!QueueUrls.TryGetValue(queueName, out var queueUrl))
        {
            queueUrl = await CreateQueue(queueName, cancellationToken);
        }

        await SqsClient.PurgeQueueAsync(queueUrl, cancellationToken);
    }

    /// <summary>
    /// Ensures the named queue exists and returns its URL, caching the result.
    /// </summary>
    /// <param name="queueName">The SQS queue name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The queue URL.</returns>
    /// <remarks>Existing queues are matched by URL suffix among the queues listed with <paramref name="queueName"/> as prefix.</remarks>
    public static async Task<string> CreateQueue(string queueName, CancellationToken cancellationToken = default)
    {
        var existingQueues = await SqsClient.ListQueuesAsync(queueName, cancellationToken);

        if (existingQueues.QueueUrls == null ||
            !existingQueues.QueueUrls.Any(q => q.EndsWith(queueName)))
        {
            var queue = await SqsClient.CreateQueueAsync(queueName, cancellationToken);
            QueueUrls[queueName] = queue.QueueUrl;
        }
        else
        {
            QueueUrls[queueName] = existingQueues.QueueUrls.First(q => q.EndsWith(queueName));
        }

        return QueueUrls[queueName];
    }
}
