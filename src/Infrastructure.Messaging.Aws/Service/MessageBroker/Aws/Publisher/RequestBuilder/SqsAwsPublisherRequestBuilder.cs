using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

public sealed class SqsAwsPublisherRequestBuilder(
    AwsMessagingOptions options,
    CompressionType compressionMethod = CompressionType.Gzip)
    : AwsPublisherRequestBuilderBase<SendMessageRequest, SendMessageBatchRequest, MessageAttributeValue>(
        compressionMethod)
{
    private const string TemplateDestinationAddress = "https://sqs.{0}.amazonaws.com/{1}/{2}";

    protected override SendMessageRequest CreateEmptyRequest() => new();

    protected override MessageAttributeValue AwsAttribute(string value) =>
        new MessageAttributeValue { DataType = "String", StringValue = value };

    protected override MessageAttributeValue AwsAttribute(byte[] value) =>
        new MessageAttributeValue { DataType = "Binary", BinaryValue = new MemoryStream(value) };

    protected override string BuildDestinationAddress(string destinationName) => string.Format(
        TemplateDestinationAddress, options.Region, options.AccountId, destinationName);

    protected override void SetDestination(SendMessageRequest request, string destinationAddress) =>
        request.SetDestination(destinationAddress);

    protected override void SetMessage(SendMessageRequest request, string body) => request.SetMessage(body);

    protected override void SetFifoProps(SendMessageRequest request, string? groupId, string? deduplicationId) =>
        request.SetFifoInfo(groupId, deduplicationId);

    protected override void SetAttributes(SendMessageRequest request,
                                          Dictionary<string, MessageAttributeValue> attrs) =>
        request.SetAttributes(attrs);

    protected override long AttributeByteSize(string key, MessageAttributeValue value)
    {
        var size = key.Utf8ByteCount()
            + value.DataType?.Utf8ByteCount() ?? 0
            + value.StringValue?.Utf8ByteCount() ?? 0;

        if (value.BinaryValue != null) size += (int)value.BinaryValue.Length;

        return size;
    }

    protected override SendMessageBatchRequest CreateBatchRequest(OutboundMessageEntity[] messages)
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

    private SendMessageBatchRequestEntry CreateBatchEntry(string id, OutboundMessageEntity message)
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
