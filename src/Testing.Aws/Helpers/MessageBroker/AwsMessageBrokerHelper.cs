using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Bootstrap.Aws.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Testing.Aws.Helpers.MessageBroker;

/// <summary>
/// Integration-testing helper for creating and wiring up AWS SNS topics and SQS queues
/// using the real AWS SDK (typically against LocalStack). Dispose it to release the SNS and SQS clients.
/// </summary>
/// <remarks>
/// Every operation sanitizes resource names (invalid characters become <c>-</c>, the <c>.fifo</c> suffix is preserved),
/// logs progress through the injected <see cref="AppTelemetry"/>, and rethrows SDK errors after logging them. Names that are
/// <see langword="null"/> or whitespace cause an <see cref="EmptyArgumentException"/>. Credentials, endpoint and region
/// come from <see cref="IAwsAppSettings.Aws"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var broker = new AwsMessageBrokerHelper(TestHelper.TestSettings&lt;AppSettings&gt;(), FixtureTelemetry.Telemetry);
/// await broker.PrepareTopicAsync("orders.fifo");
/// await broker.PrepareQueueAsync("orders-consumer.fifo");
/// await broker.SubscribeSqsQueueToSnsTopicAsync("orders-consumer.fifo", "orders.fifo");
/// var response = await broker.ReceiveMessagesAsync("orders-consumer.fifo", waitTimeSeconds: 5);
/// </code>
/// </example>
public class AwsMessageBrokerHelper : IDisposable
{
    private const string FifoSuffix = ".fifo";

    /// <summary>The SNS client, configured from <see cref="IAwsAppSettings.Aws"/>.</summary>
    public readonly AmazonSimpleNotificationServiceClient ClientSns;
    /// <summary>
    /// The SQS client, configured from <see cref="IAwsAppSettings.Aws"/>. Note that it uses the SNS endpoint setting as service URL
    /// (fine for LocalStack, where both share one endpoint).
    /// </summary>
    public readonly AmazonSQSClient ClientSqs;

    private readonly AppTelemetry _telemetry;

    /// <summary>
    /// Initializes a new instance of <see cref="AwsMessageBrokerHelper"/> using the provided
    /// <see cref="IAwsAppSettings"/>.
    /// </summary>
    /// <param name="appSettings">
    /// The application settings, for example <c>TestHelper.TestSettings&lt;TAppSettings&gt;()</c>.
    /// </param>
    /// <param name="telemetry">Receives the progress logs; discarded when <see langword="null"/>.</param>
    public AwsMessageBrokerHelper(IAwsAppSettings appSettings, AppTelemetry? telemetry = null)
    {
        _telemetry = telemetry ?? AppTelemetry.Null;

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

    private void Log(LogLevel level, string message, object? context = null,
                     [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
    {
        var callSite = new Anarchy.Toolkit.Core.Telemetry.CallSite(nameof(AwsMessageBrokerHelper), methodName);
        if (context is Exception exception)
            _telemetry.Log(level, callSite, exception, "{Message}", message);
        else if (context is not null)
            _telemetry.Log(level, callSite, null, "{Message} {@Context}", message, context);
        else
            _telemetry.Log(level, callSite, null, "{Message}", message);
    }

    /// <summary>Disposes the SNS and SQS clients.</summary>
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
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task PrepareTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var sanitizedTopicName = SanitizeResourceName(topicName);

        Log(LogLevel.Information, $"Preparing SNS topic '{sanitizedTopicName}'");

        try
        {
            var existingTopic = await ClientSns.FindTopicAsync(sanitizedTopicName);
            if (existingTopic != null)
            {
                Log(LogLevel.Debug, $"SNS topic '{sanitizedTopicName}' already exists. Deleting it first...");

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
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedTopicName = SanitizeResourceName(topicName);

            Log(LogLevel.Information, $"Deleting SNS topic '{sanitizedTopicName}'");

            var topic = await ClientSns.FindTopicAsync(sanitizedTopicName);
            if (topic == null)
            {
                Log(LogLevel.Debug, $"SNS topic '{sanitizedTopicName}' does not exist. Nothing to delete.");

                return;
            }

            await ClientSns.DeleteTopicAsync(topic.TopicArn, cancellationToken);

            Log(LogLevel.Debug, $"SNS topic '{sanitizedTopicName}' deleted.");
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while deleting SNS topic '{topicName}'.", ex);

            throw;
        }
    }

    /// <summary>
    /// Creates an SNS topic with the specified name.
    /// The name is sanitized and FIFO attributes are applied when applicable.
    /// </summary>
    /// <param name="topicName">The (possibly unsanitized) topic name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var sanitizedTopicName = SanitizeResourceName(topicName);

        Log(LogLevel.Information, "Creating SNS topic...", sanitizedTopicName);

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
                Log(LogLevel.Error, "Creation Failed!");
            }
            else
            {
                Log(LogLevel.Debug, "Topic created:", result);
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while creating SNS topic '{sanitizedTopicName}'.", ex);

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
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task SubscribeSqsQueueToSnsTopicAsync(string queueName, string topicName, CancellationToken cancellationToken = default)
    {
        var sanitizedQueueName = SanitizeResourceName(queueName);
        var sanitizedTopicName = SanitizeResourceName(topicName);

        Log(LogLevel.Information, $"Subscribing queue {sanitizedQueueName} to topic {sanitizedTopicName}");

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
                Log(LogLevel.Error, "Subscription Failed!");
                return;
            }

            // Ensure SNS message attributes become real SQS message attributes
            await ClientSns.SetSubscriptionAttributesAsync(
                new SetSubscriptionAttributesRequest {
                    SubscriptionArn = subscriptionArn,
                    AttributeName = "RawMessageDelivery",
                    AttributeValue = "true"
                }, cancellationToken);

            Log(LogLevel.Debug, "Subscription successfully created with resource name:", subscriptionArn);
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while subscribing queue '{queueName}' to topic '{topicName}'.", ex);

            throw;
        }
    }

    /// <summary>
    /// Resolves the URL of the given SQS queue, after sanitizing its name.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The resolved SQS queue URL.</returns>
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
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task CreateQueueAsync(string queueName, bool contentBasedDeduplication = true, CancellationToken cancellationToken = default)
    {
        var sanitizedQueueName = SanitizeResourceName(queueName);

        Log(LogLevel.Information, $"Creating queue '{sanitizedQueueName}'");

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
                Log(LogLevel.Error, "Queue creation has failed!");
            }
            else
            {
                Log(LogLevel.Debug, "Queue created!", result);
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while creating SQS queue '{sanitizedQueueName}'.", ex);

            throw;
        }
    }

    /// <summary>
    /// Purges all messages from the specified SQS queue.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        Log(LogLevel.Information, $"Purging queue '{queueName}'");

        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

            // Use the overload that only needs the URL when available.
            var result = await ClientSqs.PurgeQueueAsync(queueUrl, cancellationToken);

            if (result == null)
            {
                Log(LogLevel.Error, "Could not purge queue!");
            }
            else
            {
                Log(LogLevel.Debug, "Queue purged!");
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while purging SQS queue '{queueName}'.", ex);

            throw;
        }
    }

    /// <summary>
    /// Deletes an SQS queue.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        Log(LogLevel.Information, $"Purging queue '{queueName}'");

        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

            // Use the overload that only needs the URL when available.
            var result = await ClientSqs.DeleteQueueAsync(queueUrl, cancellationToken);

            if (result == null)
            {
                Log(LogLevel.Error, "Could not delete queue!");
            }
            else
            {
                Log(LogLevel.Debug, "Queue deleted!");
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while deleting SQS queue '{queueName}'.", ex);

            throw;
        }
    }

    /// <summary>
    /// Checks whether the specified SQS queue exists.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when the queue exists; otherwise <c>false</c>.</returns>
    public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
            return !string.IsNullOrEmpty(queueUrl);
        }
        catch (QueueDoesNotExistException)
        {
            Log(LogLevel.Debug, $"Queue '{queueName}' does not exist.");

            return false;
        }
        catch (AmazonSQSException ex)
        {
            Log(LogLevel.Error, $"AmazonSQSException while checking if queue '{queueName}' exists.", ex);

            throw;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Unexpected exception while checking if queue '{queueName}' exists.", ex);

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
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The <see cref="ReceiveMessageResponse"/> from SQS.</returns>
    public async Task<ReceiveMessageResponse> ReceiveMessagesAsync(string queueName, int waitTimeSeconds = 0, CancellationToken cancellationToken = default)
    {
        Log(LogLevel.Information, $"Retrieving messages from queue '{queueName}'...");

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
            Log(LogLevel.Error, $"Exception while receiving messages from SQS queue '{queueName}'.", ex);

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
    /// <returns>A task that completes when the batch has been sent.</returns>
    /// <exception cref="MessageBatchPublishException">One or more messages in the batch failed to be sent.</exception>
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
                Log(LogLevel.Error, $"Failed to publish to queue '{queueName}'", response.Failed);

                throw new MessageBatchPublishException(response.Failed.Count, destination: queueName);
            }
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, $"Exception while sending messages to SQS queue '{queueName}'.", ex);

            throw;
        }
    }

    /// <summary>
    /// Ensures the specified SQS queue exists and is empty.
    /// If it exists, it is purged; otherwise it is created.
    /// </summary>
    /// <param name="queueName">The (possibly unsanitized) queue name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
            Log(LogLevel.Error, $"Exception while preparing SQS queue '{queueName}'.", ex);

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
