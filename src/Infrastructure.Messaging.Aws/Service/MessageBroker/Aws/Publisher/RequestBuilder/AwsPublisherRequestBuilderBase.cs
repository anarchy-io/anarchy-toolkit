using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

public abstract class AwsPublisherRequestBuilderBase<TRequest, TBatchRequest, TAttr>(
    CompressionType compressionMethod = CompressionType.Gzip) : IAwsPublisherRequestBuilder<TRequest, TBatchRequest>
{
    /// <remarks>
    /// AWS SNS/SQS message size limit is 256KB (including attributes)
    /// </remarks>
    protected const long AwsMessageMaxSize = 262_144;

    protected abstract TRequest CreateEmptyRequest();
    protected abstract TAttr AwsAttribute(string value);
    protected abstract TAttr AwsAttribute(byte[] value);

    protected abstract string BuildDestinationAddress(string destinationName);

    protected abstract void SetDestination(TRequest request, string address);
    protected abstract void SetMessage(TRequest request, string body);
    protected abstract void SetFifoProps(TRequest request, string? groupId, string? dedupId);
    protected abstract void SetAttributes(TRequest request, Dictionary<string, TAttr> attrs);

    protected abstract long AttributeByteSize(string key, TAttr value);
    protected abstract TBatchRequest CreateBatchRequest(OutboundMessageEntity[] messages);

    private Dictionary<string, TAttr> ConvertAttributes(OutboundMessageEntity input)
    {
        var result = new Dictionary<string, TAttr>(StringComparer.Ordinal);
        foreach (var kvp in input.Attributes)
        {
            result[kvp.Key] = AwsAttribute(kvp.Value);
        }

        return result;
    }

    private long CalculateRequestSizeBytes(string body, Dictionary<string, TAttr> attrs)
    {
        return attrs.Aggregate<KeyValuePair<string, TAttr>, long>(body.Utf8ByteCount(),
            (current, kvp) => current + AttributeByteSize(kvp.Key, kvp.Value));
    }

    public TRequest FromOutboundMessage(OutboundMessageEntity message)
    {
        var result = CreateEmptyRequest();

        SetDestination(result, BuildDestinationAddress(message.Destination));

        if (message.Destination.IsFifo())
        {
            SetFifoProps(result, message.PartitionKey, message.DeduplicationId);
        }

        var resultAttributes = ConvertAttributes(message);

        if (CalculateRequestSizeBytes(message.Message, resultAttributes) < AwsMessageMaxSize)
        {
            SetMessage(result, message.Message);
        }
        else
        {
            if (compressionMethod == CompressionType.Uncompressed)
            {
                throw new UncompressedMessageTooLargeException(AwsMessageMaxSize);
            }

            resultAttributes["CompressionType"] = AwsAttribute(compressionMethod.ToDescription());
            resultAttributes["CompressionData"] = AwsAttribute(
                CompressionHelper.CompressFromUtf8(message.Message.ToJson(), compressionMethod)
            );

            SetMessage(result, string.Empty);
        }

        SetAttributes(result, resultAttributes);

        return result;
    }

    public TBatchRequest FromOutboundMessage(OutboundMessageEntity[] messages)
    {
        return CreateBatchRequest(messages);
    }

    protected void PopulateRequestFromMessage(TRequest request, OutboundMessageEntity message)
    {
        SetDestination(request, BuildDestinationAddress(message.Destination));

        if (message.Destination.IsFifo())
        {
            SetFifoProps(request, message.PartitionKey, message.DeduplicationId);
        }

        var resultAttributes = ConvertAttributes(message);

        if (CalculateRequestSizeBytes(message.Message, resultAttributes) < AwsMessageMaxSize)
        {
            SetMessage(request, message.Message);
        }
        else
        {
            if (compressionMethod == CompressionType.Uncompressed)
            {
                throw new UncompressedMessageTooLargeException(AwsMessageMaxSize);
            }

            resultAttributes["CompressionType"] = AwsAttribute(compressionMethod.ToDescription());
            resultAttributes["CompressionData"] = AwsAttribute(
                CompressionHelper.CompressFromUtf8(message.Message.ToJson(), compressionMethod)
            );

            SetMessage(request, string.Empty);
        }

        SetAttributes(request, resultAttributes);
    }
}
