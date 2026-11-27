using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.Compression;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.ValueSet.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;

/// <summary>
/// Template base class for AWS publisher request builders. Converts an <see cref="OutboundMessage"/> into an
/// SDK request, handling destination addressing, FIFO properties, attribute conversion and transparent compression of
/// oversized messages.
/// </summary>
/// <remarks>
/// <para>
/// Derived classes supply the transport-specific pieces (request creation, attribute values, destination address,
/// setters and batch creation). The algorithm in <see cref="FromOutboundMessage(OutboundMessage)"/> is:
/// </para>
/// <list type="number">
///   <item><description>Set the destination from <see cref="BuildDestinationAddress"/>.</description></item>
///   <item><description>For FIFO destinations (names ending in <c>.fifo</c>), set the partition key as the group id
///   and the deduplication id.</description></item>
///   <item><description>Convert all message attributes to string attributes.</description></item>
///   <item><description>If body plus attributes is below <see cref="AwsMessageMaxSize"/>, send the body as is.
///   Otherwise compress the JSON-serialized body into a binary <c>CompressionData</c> attribute, add a
///   <c>CompressionType</c> attribute and send an empty body. Consumers can restore it with
///   <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker.InboundMessageMapper"/>.</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TRequest">The AWS single-message request type.</typeparam>
/// <typeparam name="TBatchRequest">The AWS batch request type.</typeparam>
/// <typeparam name="TAttr">The AWS message attribute value type.</typeparam>
/// <param name="compressionMethod">
/// Compression applied to oversized messages. With <see cref="CompressionType.Uncompressed"/>, oversized messages are
/// rejected with <see cref="UncompressedMessageTooLargeException"/>. Defaults to <see cref="CompressionType.Gzip"/>.
/// </param>
public abstract class AwsPublisherRequestBuilderBase<TRequest, TBatchRequest, TAttr>(
    CompressionType compressionMethod = CompressionType.Gzip) : IAwsPublisherRequestBuilder<TRequest, TBatchRequest>
{
    /// <summary>
    /// Maximum size, in bytes, of a message (body plus attributes) sent uncompressed: 262,144 bytes (256 KB).
    /// </summary>
    /// <remarks>
    /// AWS SNS/SQS message size limit is 256KB (including attributes)
    /// </remarks>
    protected const long AwsMessageMaxSize = 262_144;

    /// <summary>Creates a new, empty single-message request.</summary>
    /// <returns>The new request.</returns>
    protected abstract TRequest CreateEmptyRequest();

    /// <summary>Creates a string-typed message attribute value.</summary>
    /// <param name="value">The attribute value.</param>
    /// <returns>The AWS attribute value.</returns>
    protected abstract TAttr AwsAttribute(string value);

    /// <summary>Creates a binary-typed message attribute value.</summary>
    /// <param name="value">The attribute bytes.</param>
    /// <returns>The AWS attribute value.</returns>
    protected abstract TAttr AwsAttribute(byte[] value);

    /// <summary>Builds the transport address (topic ARN or queue URL) for a logical destination name.</summary>
    /// <param name="destinationName">The destination name from <see cref="OutboundMessage.Destination"/>.</param>
    /// <returns>The full destination address.</returns>
    protected abstract string BuildDestinationAddress(string destinationName);

    /// <summary>Sets the destination address on the request.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="address">The address produced by <see cref="BuildDestinationAddress"/>.</param>
    protected abstract void SetDestination(TRequest request, string address);

    /// <summary>Sets the message body on the request.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="body">The body (empty when the payload was moved to a compressed attribute).</param>
    protected abstract void SetMessage(TRequest request, string body);

    /// <summary>Sets FIFO properties on the request.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="groupId">The message group id (the message partition key).</param>
    /// <param name="dedupId">The deduplication id.</param>
    protected abstract void SetFifoProps(TRequest request, string? groupId, string? dedupId);

    /// <summary>Sets the message attributes on the request.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="attrs">The attributes, keyed by name.</param>
    protected abstract void SetAttributes(TRequest request, Dictionary<string, TAttr> attrs);

    /// <summary>Computes the size, in bytes, an attribute contributes to the message size limit.</summary>
    /// <param name="key">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The attribute size in bytes.</returns>
    protected abstract long AttributeByteSize(string key, TAttr value);

    /// <summary>Creates a batch request for <paramref name="messages"/>.</summary>
    /// <param name="messages">The messages in the batch, all sharing the same destination.</param>
    /// <returns>The batch request.</returns>
    protected abstract TBatchRequest CreateBatchRequest(OutboundMessage[] messages);

    private Dictionary<string, TAttr> ConvertAttributes(OutboundMessage input)
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

    /// <inheritdoc/>
    /// <exception cref="UncompressedMessageTooLargeException">
    /// The message exceeds <see cref="AwsMessageMaxSize"/> and compression is <see cref="CompressionType.Uncompressed"/>.
    /// </exception>
    public TRequest FromOutboundMessage(OutboundMessage message)
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

    /// <inheritdoc/>
    /// <remarks>Delegates to <see cref="CreateBatchRequest"/>.</remarks>
    public TBatchRequest FromOutboundMessage(OutboundMessage[] messages)
    {
        return CreateBatchRequest(messages);
    }

    /// <summary>
    /// Populates an existing request from <paramref name="message"/> using the same algorithm as
    /// <see cref="FromOutboundMessage(OutboundMessage)"/>. Used by derived classes to build batch entries.
    /// </summary>
    /// <param name="request">The request to populate.</param>
    /// <param name="message">The source message.</param>
    /// <exception cref="UncompressedMessageTooLargeException">
    /// The message exceeds <see cref="AwsMessageMaxSize"/> and compression is <see cref="CompressionType.Uncompressed"/>.
    /// </exception>
    protected void PopulateRequestFromMessage(TRequest request, OutboundMessage message)
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
