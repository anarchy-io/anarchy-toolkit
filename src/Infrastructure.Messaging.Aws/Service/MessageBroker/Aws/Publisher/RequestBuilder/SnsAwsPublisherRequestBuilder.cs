using Amazon.SimpleNotificationService.Model;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

/// <summary>
/// SNS implementation of <see cref="AwsPublisherRequestBuilderBase{TRequest, TBatchRequest, TAttr}"/>: builds
/// SNS requests whose destination is the topic ARN <c>arn:aws:sns:{region}:{accountId}:{name}</c>.
/// </summary>
/// <remarks>
/// Batch requests take their destination from the first message; FIFO group/deduplication ids are set per entry when
/// the destination is FIFO. Entry ids are the zero-based index of the message in the batch.
/// </remarks>
/// <param name="options">The AWS region and account used to build destination addresses.</param>
/// <param name="compressionMethod">Compression applied to oversized messages. Defaults to <see cref="CompressionType.Gzip"/>.</param>
public sealed class SnsAwsPublisherRequestBuilder(
    AwsMessagingOptions options,
    CompressionType compressionMethod = CompressionType.Gzip)
    : AwsPublisherRequestBuilderBase<PublishRequest, PublishBatchRequest, MessageAttributeValue>(
        compressionMethod)
{
    private const string TemplateDestinationAddress = "arn:aws:sns:{0}:{1}:{2}";

    /// <inheritdoc/>
    protected override PublishRequest CreateEmptyRequest() => new();

    /// <inheritdoc/>
    protected override MessageAttributeValue AwsAttribute(string value) => new() { DataType = "String", StringValue = value };

    /// <inheritdoc/>
    protected override MessageAttributeValue AwsAttribute(byte[] value) =>
        new() { DataType = "Binary", BinaryValue = new MemoryStream(value) };

    /// <inheritdoc/>
    protected override string BuildDestinationAddress(string destinationName) => string.Format(
        TemplateDestinationAddress, options.Region, options.AccountId, destinationName);

    /// <inheritdoc/>
    protected override void SetDestination(PublishRequest request, string destinationAddress) =>
        request.SetDestination(destinationAddress);

    /// <inheritdoc/>
    protected override void SetMessage(PublishRequest request, string body) => request.SetMessage(body);

    /// <inheritdoc/>
    protected override void SetFifoProps(PublishRequest request, string? groupId, string? deduplicationId) =>
        request.SetFifoInfo(groupId, deduplicationId);

    /// <inheritdoc/>
    protected override void SetAttributes(PublishRequest request, Dictionary<string, MessageAttributeValue> attrs) =>
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
    protected override PublishBatchRequest CreateBatchRequest(OutboundMessageEntity[] messages)
    {
        if (messages.Length == 0)
            throw new EmptyArgumentException(nameof(messages));

        // Get the destination from the first message (all should have same destination)
        var destinationTopicArn = BuildDestinationAddress(messages[0].Destination);

        var publishBatchRequestEntries = messages
            .Select((message, index) => CreateBatchEntry(index.ToString(), message))
            .ToList();

        return new PublishBatchRequest {
            TopicArn = destinationTopicArn,
            PublishBatchRequestEntries = publishBatchRequestEntries
        };
    }

    private PublishBatchRequestEntry CreateBatchEntry(string id, OutboundMessageEntity message)
    {
        var entry = new PublishBatchRequestEntry { Id = id };

        // Use PopulateRequestFromMessage to build the message data properly
        var tempRequest = new PublishRequest();
        PopulateRequestFromMessage(tempRequest, message);

        entry.Message = tempRequest.Message;
        entry.MessageAttributes = tempRequest.MessageAttributes;

        if (message.Destination.IsFifo())
        {
            entry.MessageGroupId = message.PartitionKey;
            entry.MessageDeduplicationId = message.DeduplicationId;
        }

        return entry;
    }
}
