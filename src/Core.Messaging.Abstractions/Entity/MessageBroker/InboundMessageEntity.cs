using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Exceptions;
using System.Text;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

public record InboundMessageEntity : IEntity
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

    public InboundMessageEntity(
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

    public InboundMessageEntity(
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
