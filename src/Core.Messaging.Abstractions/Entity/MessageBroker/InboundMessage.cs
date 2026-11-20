using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Exceptions;
using System.Text;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

/// <summary>
/// A broker-agnostic message received from a message broker.
/// </summary>
/// <remarks>
/// Broker adapters map their native messages into this type so consumers (e.g. event envelope parsers) stay
/// independent of the broker SDK. The message body is always stored as a string; byte buffers are decoded as UTF-8.
/// </remarks>
public record InboundMessage : IEntity
{
    ///<summary>
    /// Unique identifier of the message
    ///</summary>
    public readonly string Id;

    ///<summary>
    /// Unique broker reference of the received message
    ///</summary>
    public readonly string ReceiptHandle;

    ///<summary>
    /// The message body that is consumed from the broker
    ///</summary>
    public readonly string Message;

    ///<summary>
    /// The location the message is received from. This can be a queue or topic name for example
    ///</summary>
    public readonly string Source;

    ///<summary>
    /// The partition the message belongs to
    ///</summary>
    public readonly string? PartitionKey;

    ///<summary>
    /// Custom meta-data attributes that are attached to the message
    ///</summary>
    public readonly IReadOnlyDictionary<string, string> Attributes;

    /// <summary>
    /// Creates an inbound message from a UTF-8 encoded body.
    /// </summary>
    /// <param name="id">Unique identifier of the message. Required.</param>
    /// <param name="receiptHandle">Broker reference used to acknowledge/delete the message. Required.</param>
    /// <param name="messageBuffer">The UTF-8 encoded message body. Must not be empty.</param>
    /// <param name="source">The queue/topic name the message was received from. Required.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom attributes; defaults to an empty dictionary.</param>
    /// <exception cref="EmptyArgumentException">
    /// <paramref name="messageBuffer"/> is empty, or any required value is null, empty or whitespace.
    /// </exception>
    public InboundMessage(
        string id,
        string receiptHandle,
        ReadOnlySpan<byte> messageBuffer,
        string source,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        : this(
            id,
            receiptHandle,
            messageBuffer.IsEmpty ?
                throw new EmptyArgumentException(nameof(messageBuffer)) :
                Encoding.UTF8.GetString(messageBuffer),
            source,
            partitionKey,
            attributes)
    {
    }

    /// <summary>
    /// Creates an inbound message.
    /// </summary>
    /// <param name="id">Unique identifier of the message. Required.</param>
    /// <param name="receiptHandle">Broker reference used to acknowledge/delete the message. Required.</param>
    /// <param name="message">The message body. Required.</param>
    /// <param name="source">The queue/topic name the message was received from. Required.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom attributes; defaults to an empty dictionary.</param>
    /// <exception cref="EmptyArgumentException">
    /// <paramref name="id"/>, <paramref name="receiptHandle"/>, <paramref name="message"/> or
    /// <paramref name="source"/> is null, empty or whitespace.
    /// </exception>
    public InboundMessage(
        string id,
        string receiptHandle,
        string message,
        string source,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new EmptyArgumentException(nameof(id));

        if (string.IsNullOrWhiteSpace(receiptHandle))
            throw new EmptyArgumentException(nameof(receiptHandle));

        if (string.IsNullOrWhiteSpace(message))
            throw new EmptyArgumentException(nameof(message));

        if (string.IsNullOrWhiteSpace(source))
            throw new EmptyArgumentException(nameof(source));

        Id = id;
        ReceiptHandle = receiptHandle;
        Message = message;
        Source = source;
        PartitionKey = partitionKey;
        Attributes = attributes ?? new Dictionary<string, string>();
    }
}
