using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Testing.Aws.Helper;

/// <summary>
/// Integration-testing helper for creating and wiring up AWS SNS topics and SQS queues
/// using the real AWS SDK (typically against LocalStack). Dispose it to release the SNS and SQS clients.
/// </summary>
public class AwsMessageBrokerHelper : IDisposable
{
    private const string DevLogPrefix = "AWS Helper";
    private const string FifoSuffix = ".fifo";

    public readonly AmazonSimpleNotificationServiceClient ClientSns;
    public readonly AmazonSQSClient ClientSqs;

    /// <summary>
    /// Initializes a new instance of <see cref="AwsMessageBrokerHelper"/> using the provided
    /// <see cref="IAwsAppSettings"/>.
    /// </summary>
    /// <param name="appSettings">
    /// The application settings, for example <c>TestHelper.TestSettings&lt;TAppSettings&gt;()</c>.
    /// </param>
    public AwsMessageBrokerHelper(IAwsAppSettings appSettings)
    {
        ClientSns = new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials(
            appSettings.Aws.AccessKey,
            appSettings.Aws.SecretKey
        ), new AmazonSimpleNotificationServiceConfig {
            ServiceURL = appSettings.Aws.EndpointSns,
            AuthenticationRegion = appSettings.Aws.RegionName
        });

        ClientSqs = new AmazonSQSClient(new BasicAWSCredentials(
            appSettings.Aws.AccessKey,
            appSettings.Aws.SecretKey
        ), new AmazonSQSConfig {
            ServiceURL = appSettings.Aws.EndpointSns,
            AuthenticationRegion = appSettings.Aws.RegionName
        });
    }

    public void Dispose()
    {
        ClientSns.Dispose();
        ClientSqs.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Ensures the specified SNS topic exists and is in a clean state for testing.
    /// If it already exists, it is deleted and then recreated; otherwise it is created.
    /// </summary>
    /// <param name="topicName">The (possibly unsanitized) topic name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task PrepareTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var sanitizedTopicName = SanitizeResourceName(topicName);

        DevelopmentLogger.Print(
            $"Preparing SNS topic '{sanitizedTopicName}'",
            DevLogPrefix,
            DevelopmentLoggerLevel.Info);

        try
        {
            var existingTopic = await ClientSns.FindTopicAsync(sanitizedTopicName);
            if (existingTopic != null)
            {
                DevelopmentLogger.Print(
                    $"SNS topic '{sanitizedTopicName}' already exists. Deleting it first...",
                    DevLogPrefix,
                    DevelopmentLoggerLevel.Debug);

                await ClientSns.DeleteTopicAsync(existingTopic.TopicArn, cancellationToken);
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            await CreateTopicAsync(sanitizedTopicName, cancellationToken);
        }
    }

    /// <summary>
    /// Deletes the specified SNS topic, if it exists.
    /// </summary>
    /// <param name="topicName">The (possibly unsanitized) topic name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedTopicName = SanitizeResourceName(topicName);

            DevelopmentLogger.Print(
                $"Deleting SNS topic '{sanitizedTopicName}'",
                DevLogPrefix,
                DevelopmentLoggerLevel.Info);

            var topic = await ClientSns.FindTopicAsync(sanitizedTopicName);
            if (topic == null)
            {
                DevelopmentLogger.Print(
                    $"SNS topic '{sanitizedTopicName}' does not exist. Nothing to delete.",
                    DevLogPrefix,
                    DevelopmentLoggerLevel.Debug);

                return;
            }

            await ClientSns.DeleteTopicAsync(topic.TopicArn, cancellationToken);

            DevelopmentLogger.Print(
                $"SNS topic '{sanitizedTopicName}' deleted.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Debug);
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while deleting SNS topic '{topicName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Creates an SNS topic with the specified name.
    /// The name is sanitized and FIFO attributes are applied when applicable.
    /// </summary>
    /// <param name="topicName">The (possibly unsanitized) topic name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var sanitizedTopicName = SanitizeResourceName(topicName);

        DevelopmentLogger.Print("Creating SNS topic...", DevLogPrefix, DevelopmentLoggerLevel.Info, sanitizedTopicName);

        try
        {
            var request = new CreateTopicRequest(sanitizedTopicName);

            if (IsFifo(sanitizedTopicName))
            {
                request.Attributes["FifoTopic"] = "true";
                request.Attributes["ContentBasedDeduplication"] = "true";
            }

            var result = await ClientSns.CreateTopicAsync(request, cancellationToken);

            if (result == null)
            {
                DevelopmentLogger.Print("Creation Failed!", DevLogPrefix, DevelopmentLoggerLevel.Error);
            }
            else
            {
                DevelopmentLogger.Print("Topic created:", DevLogPrefix, DevelopmentLoggerLevel.Debug, result);
            }
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while creating SNS topic '{sanitizedTopicName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Subscribes the given SQS queue to the given SNS topic.
    /// Names are sanitized and resolved using <c>FindTopicAsync</c> and <c>GetQueueUrlAsync</c>.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="topicName">The (possibly unsanitized) topic name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task SubscribeSqsQueueToSnsTopicAsync(string queueName, string topicName, CancellationToken cancellationToken = default)
    {
        var sanitizedQueueName = SanitizeResourceName(queueName);
        var sanitizedTopicName = SanitizeResourceName(topicName);

        DevelopmentLogger.Print(
            $"Subscribing queue {sanitizedQueueName} to topic {sanitizedTopicName}",
            DevLogPrefix,
            DevelopmentLoggerLevel.Info);

        try
        {
            var topic = await ClientSns.FindTopicAsync(sanitizedTopicName);
            var queueUrlResponse = await ClientSqs.GetQueueUrlAsync(sanitizedQueueName, cancellationToken);

            var subscriptionArn = await ClientSns.SubscribeQueueAsync(
                topic.TopicArn,
                ClientSqs,
                queueUrlResponse.QueueUrl);

            if (subscriptionArn == null)
            {
                DevelopmentLogger.Print("Subscription Failed!", DevLogPrefix, DevelopmentLoggerLevel.Error);
                return;
            }

            // Ensure SNS message attributes become real SQS message attributes
            await ClientSns.SetSubscriptionAttributesAsync(
                new SetSubscriptionAttributesRequest {
                    SubscriptionArn = subscriptionArn,
                    AttributeName = "RawMessageDelivery",
                    AttributeValue = "true"
                }, cancellationToken);

            DevelopmentLogger.Print(
                "Subscription successfully created with resource name:",
                DevLogPrefix,
                DevelopmentLoggerLevel.Debug,
                subscriptionArn);
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while subscribing queue '{queueName}' to topic '{topicName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Resolves the URL of the given SQS queue, after sanitizing its name.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <returns>The resolved SQS queue URL.</returns>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    private async Task<string> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var sanitizedQueueName = SanitizeResourceName(queueName);
        var response = await ClientSqs.GetQueueUrlAsync(sanitizedQueueName, cancellationToken);
        return response.QueueUrl;
    }

    /// <summary>
    /// Creates an SQS queue with the specified name.
    /// The name is sanitized and FIFO attributes are applied when applicable.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="contentBasedDeduplication">
    /// When <c>true</c> and the queue is FIFO, <c>ContentBasedDeduplication</c> is enabled.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task CreateQueueAsync(string queueName, bool contentBasedDeduplication = true, CancellationToken cancellationToken = default)
    {
        var sanitizedQueueName = SanitizeResourceName(queueName);

        DevelopmentLogger.Print(
            $"Creating queue '{sanitizedQueueName}'",
            DevLogPrefix,
            DevelopmentLoggerLevel.Info);

        try
        {
            var request = new CreateQueueRequest {
                QueueName = sanitizedQueueName,
                Attributes = new Dictionary<string, string> { { "VisibilityTimeout", "0" } }
            };

            if (IsFifo(sanitizedQueueName))
            {
                request.Attributes["FifoQueue"] = "true";
                request.Attributes["ContentBasedDeduplication"] = contentBasedDeduplication ? "true" : "false";
            }

            var result = await ClientSqs.CreateQueueAsync(request, cancellationToken);

            if (result == null)
            {
                DevelopmentLogger.Print("Queue creation has failed!", DevLogPrefix, DevelopmentLoggerLevel.Error);
            }
            else
            {
                DevelopmentLogger.Print("Queue created!", DevLogPrefix, DevelopmentLoggerLevel.Debug, result);
            }
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while creating SQS queue '{sanitizedQueueName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Purges all messages from the specified SQS queue.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        DevelopmentLogger.Print(
            $"Purging queue '{queueName}'",
            DevLogPrefix,
            DevelopmentLoggerLevel.Info);

        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

            // Use the overload that only needs the URL when available.
            var result = await ClientSqs.PurgeQueueAsync(queueUrl, cancellationToken);

            if (result == null)
            {
                DevelopmentLogger.Print("Could not purge queue!", DevLogPrefix, DevelopmentLoggerLevel.Error);
            }
            else
            {
                DevelopmentLogger.Print("Queue purged!", DevLogPrefix, DevelopmentLoggerLevel.Debug);
            }
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while purging SQS queue '{queueName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Deletes an SQS queue.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        DevelopmentLogger.Print(
            $"Purging queue '{queueName}'",
            DevLogPrefix,
            DevelopmentLoggerLevel.Info);

        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

            // Use the overload that only needs the URL when available.
            var result = await ClientSqs.DeleteQueueAsync(queueUrl, cancellationToken);

            if (result == null)
            {
                DevelopmentLogger.Print("Could not delete queue!", DevLogPrefix, DevelopmentLoggerLevel.Error);
            }
            else
            {
                DevelopmentLogger.Print("Queue deleted!", DevLogPrefix, DevelopmentLoggerLevel.Debug);
            }
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while deleting SQS queue '{queueName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Checks whether the specified SQS queue exists.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <returns><c>true</c> when the queue exists; otherwise <c>false</c>.</returns>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
            return !string.IsNullOrEmpty(queueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            DevelopmentLogger.Print(
                $"Queue '{queueName}' does not exist.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Debug);

            return false;
        }
        catch (AmazonSQSException ex)
        {
            DevelopmentLogger.Print(
                $"AmazonSQSException while checking if queue '{queueName}' exists.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Unexpected exception while checking if queue '{queueName}' exists.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Receives up to 10 messages from the specified SQS queue.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="waitTimeSeconds">
    /// Long-poll wait time in seconds; capped at 20 seconds and defaults to 0.
    /// </param>
    /// <returns>The <see cref="ReceiveMessageResponse"/> from SQS.</returns>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<ReceiveMessageResponse> ReceiveMessagesAsync(string queueName, int waitTimeSeconds = 0, CancellationToken cancellationToken = default)
    {
        DevelopmentLogger.Print(
            $"Retrieving messages from queue '{queueName}'...",
            DevLogPrefix,
            DevelopmentLoggerLevel.Info);

        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

            var request = new ReceiveMessageRequest {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                MessageAttributeNames = ["All"],
                WaitTimeSeconds = Math.Min(20, waitTimeSeconds),
                VisibilityTimeout = 0
            };

            return await ClientSqs.ReceiveMessageAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while receiving messages from SQS queue '{queueName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Sends a batch of messages to the specified SQS queue.
    /// For FIFO queues, <c>MessageGroupId</c> is set using <paramref name="partitionKey"/> (or "default").
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="messages">The messages to send (will be serialized to JSON).</param>
    /// <param name="partitionKey">
    /// Optional partition key used as <c>MessageGroupId</c> for FIFO queues; defaults to "default".
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task SendMessages(string queueName, IEnumerable<object> messages, string? partitionKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
            var isFifoQueue = IsFifo(queueName);

            var entries = messages
                .Select((message, index) => new SendMessageBatchRequestEntry {
                    Id = (index + 1).ToString(),
                    MessageBody = message.ToJson(),
                    MessageGroupId = isFifoQueue ? (partitionKey ?? "default") : null,
                    MessageDeduplicationId = Guid.NewGuid().ToString()
                })
                .ToList();

            var request = new SendMessageBatchRequest { QueueUrl = queueUrl, Entries = entries };

            var response = await ClientSqs.SendMessageBatchAsync(request, cancellationToken);

            if (response.Failed is { Count: > 0 })
            {
                DevelopmentLogger.Print(
                    $"Failed to publish to queue '{queueName}'",
                    DevLogPrefix,
                    DevelopmentLoggerLevel.Error,
                    response.Failed);

                throw new MessageBatchPublishException(response.Failed.Count, destination: queueName);
            }
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while sending messages to SQS queue '{queueName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Ensures the specified SQS queue exists and is empty.
    /// If it exists, it is purged; otherwise it is created.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task PrepareQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await QueueExistsAsync(queueName, cancellationToken))
                await PurgeQueueAsync(queueName, cancellationToken);
            else
                await CreateQueueAsync(queueName, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            DevelopmentLogger.Print(
                $"Exception while preparing SQS queue '{queueName}'.",
                DevLogPrefix,
                DevelopmentLoggerLevel.Error,
                ex);

            throw;
        }
    }

    /// <summary>
    /// Determines whether the given resource name represents a FIFO SNS/SQS resource.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <returns><c>true</c> if the name ends with ".fifo"; otherwise <c>false</c>.</returns>
    private static bool IsFifo(string name) =>
        name.EndsWith(FifoSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Sanitizes an SNS topic or SQS queue name by replacing invalid characters with '-'.
    /// Preserves the ".fifo" suffix for FIFO resources.
    /// </summary>
    /// <param name="name">The original resource name.</param>
    /// <returns>The sanitized resource name.</returns>
    /// <exception cref="EmptyArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    private static string SanitizeResourceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EmptyArgumentException(nameof(name));

        var isFifo = IsFifo(name);
        var baseName = isFifo ? name[..^FifoSuffix.Length] : name;

        // Allow only alphanumerics, underscores and dashes; everything else becomes '-'.
        var sanitizedBase = Regex.Replace(baseName, "[^A-Za-z0-9_-]", "-");

        return isFifo ? sanitizedBase + FifoSuffix : sanitizedBase;
    }
}
