using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Amazon.SQS.Model;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

/// <summary>
/// SQS implementation of <see cref="AwsPublisherRequestBuilderBase{TRequest, TBatchRequest, TAttr}"/>: builds
/// SQS requests whose destination is the queue URL <c>https://sqs.{region}.amazonaws.com/{accountId}/{name}</c>.
/// </summary>
/// <remarks>
/// Batch requests take their destination from the first message; FIFO group/deduplication ids are set per entry when
/// the destination is FIFO. Entry ids are the zero-based index of the message in the batch.
/// </remarks>
/// <param name="options">The AWS region and account used to build destination addresses.</param>
/// <param name="compressionMethod">Compression applied to oversized messages. Defaults to <see cref="CompressionType.Gzip"/>.</param>
public sealed class SqsAwsPublisherRequestBuilder(
    AwsMessagingOptions options,
    CompressionType compressionMethod = CompressionType.Gzip)
    : AwsPublisherRequestBuilderBase<SendMessageRequest, SendMessageBatchRequest, MessageAttributeValue>(
        compressionMethod)
{
    private const string TemplateDestinationAddress = "https://sqs.{0}.amazonaws.com/{1}/{2}";

    /// <inheritdoc/>
    protected override SendMessageRequest CreateEmptyRequest() => new();

    /// <inheritdoc/>
    protected override MessageAttributeValue AwsAttribute(string value) => new() { DataType = "String", StringValue = value };

    /// <inheritdoc/>
    protected override MessageAttributeValue AwsAttribute(byte[] value) =>
        new() { DataType = "Binary", BinaryValue = new MemoryStream(value) };

    /// <inheritdoc/>
    protected override string BuildDestinationAddress(string destinationName) => string.Format(
        TemplateDestinationAddress, options.Region, options.AccountId, destinationName);

    /// <inheritdoc/>
    protected override void SetDestination(SendMessageRequest request, string destinationAddress) =>
        request.SetDestination(destinationAddress);

    /// <inheritdoc/>
    protected override void SetMessage(SendMessageRequest request, string body) => request.SetMessage(body);

    /// <inheritdoc/>
    protected override void SetFifoProps(SendMessageRequest request, string? groupId, string? deduplicationId) =>
        request.SetFifoInfo(groupId, deduplicationId);

    /// <inheritdoc/>
    protected override void SetAttributes(SendMessageRequest request,
                                          Dictionary<string, MessageAttributeValue> attrs) =>
        request.SetAttributes(attrs);

    /// <inheritdoc/>
    protected override long AttributeByteSize(string key, MessageAttributeValue value)
    {
        var size = key.Utf8ByteCount()
            + value.DataType?.Utf8ByteCount() ?? 0
            + value.StringValue?.Utf8ByteCount() ?? 0;

        if (value.BinaryValue != null) size += (int)value.BinaryValue.Length;

        return size;
    }

    /// <inheritdoc/>
    /// <exception cref="EmptyArgumentException"><paramref name="messages"/> is empty.</exception>
    protected override SendMessageBatchRequest CreateBatchRequest(OutboundMessage[] messages)
    {
        if (messages.Length == 0)
            throw new EmptyArgumentException(nameof(messages));

        // Get the destination URL from the first message (all should have same destination)
        var destinationQueueUrl = BuildDestinationAddress(messages[0].Destination);

        var sendMessageBatchRequestEntries = messages
            .Select((message, index) => CreateBatchEntry(index.ToString(), message))
            .ToList();

        return new SendMessageBatchRequest {
            QueueUrl = destinationQueueUrl,
            Entries = sendMessageBatchRequestEntries
        };
    }

    private SendMessageBatchRequestEntry CreateBatchEntry(string id, OutboundMessage message)
    {
        var entry = new SendMessageBatchRequestEntry { Id = id };

        // Use PopulateRequestFromMessage to build the message data properly
        var tempRequest = new SendMessageRequest();
        PopulateRequestFromMessage(tempRequest, message);

        entry.MessageBody = tempRequest.MessageBody;
        entry.MessageAttributes = tempRequest.MessageAttributes;

        if (message.Destination.IsFifo())
        {
            entry.MessageGroupId = message.PartitionKey;
            entry.MessageDeduplicationId = message.DeduplicationId;
        }

        return entry;
    }
}
