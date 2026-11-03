using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Exceptions;
using System.Text;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

public sealed record OutboundMessageEntity : IEntity
{
    ///<summary>
    /// Unique identifier of the message
    ///</summary>
    public readonly string Id;

    ///<summary>
    /// The location the message will be published to. This can be a queue or topic name for example
    ///</summary>
    public readonly string Destination;

    ///<summary>
    /// The message body that will be published to the broker
    ///</summary>
    public readonly string Message;

    ///<summary>
    /// The partition the message belongs to
    ///</summary>
    public readonly string? PartitionKey;

    ///<summary>
    /// Unique ID to override the message broker fingerprint (if supported) of the message to detect duplicate messages
    ///</summary>
    public readonly string? DeduplicationId;

    ///<summary>
    /// Custom meta-data attributes that are attached to the message
    ///</summary>
    public readonly IReadOnlyDictionary<string, string> Attributes;

    public OutboundMessageEntity(
        string id,
        string destination,
        ReadOnlySpan<byte> messageBuffer,
        string? partitionKey = null,
        string? deduplicationId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        : this(
            id,
            destination,
            messageBuffer.IsEmpty ?
                throw new EmptyArgumentException(nameof(messageBuffer)) :
                Encoding.UTF8.GetString(messageBuffer),
            partitionKey,
            deduplicationId,
            attributes)
    {
    }

    public OutboundMessageEntity(
        string id,
        string destination,
        string message,
        string? partitionKey = null,
        string? deduplicationId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new EmptyArgumentException(nameof(id));

        if (string.IsNullOrWhiteSpace(destination))
            throw new EmptyArgumentException(nameof(destination));

        if (string.IsNullOrWhiteSpace(message))
            throw new EmptyArgumentException(nameof(message));

        if (string.IsNullOrWhiteSpace(deduplicationId))
            throw new EmptyArgumentException(nameof(deduplicationId));

        Id = id;
        Destination = destination;
        Message = message;
        DeduplicationId = deduplicationId;
        PartitionKey = partitionKey;
        Attributes = attributes ?? new Dictionary<string, string>();
    }
}
