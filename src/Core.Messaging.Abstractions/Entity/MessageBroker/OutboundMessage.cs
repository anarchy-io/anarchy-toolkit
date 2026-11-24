using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Exceptions;
using System.Collections.ObjectModel;
using System.Text;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

/// <summary>
/// A broker-agnostic message ready to be published through
/// <see cref="Service.MessageBroker.IMessageBrokerPublisher"/>.
/// </summary>
/// <remarks>
/// Use <see cref="Service.MessageBroker.OutboundMessageBuilder"/> to create instances with generated identifiers.
/// The message body is always stored as a string; byte buffers are decoded as UTF-8.
/// </remarks>
public sealed record OutboundMessage : IEntity
{
    ///<summary>
    /// Unique identifier of the message
    ///</summary>
    public readonly string Id;

    ///<summary>
    /// The broker location the message will be published to, such as a queue or topic.
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
    /// Read-only snapshot of the custom metadata attached to the message.
    ///</summary>
    public readonly IReadOnlyDictionary<string, string> Attributes;

    /// <summary>
    /// Creates an outbound message from a UTF-8 encoded body.
    /// </summary>
    /// <param name="id">Unique identifier of the message. Required.</param>
    /// <param name="destination">The queue/topic name to publish to. Required.</param>
    /// <param name="messageBuffer">The UTF-8 encoded message body. Must not be empty.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="deduplicationId">
    /// Optional deduplication identifier, interpreted only by transports that support it.
    /// </param>
    /// <param name="attributes">Optional custom attributes; defaults to an empty dictionary.</param>
    /// <exception cref="EmptyArgumentException">
    /// <paramref name="messageBuffer"/> is empty, or any required value is null, empty or whitespace.
    /// </exception>
    public OutboundMessage(
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

    /// <summary>
    /// Creates an outbound message.
    /// </summary>
    /// <param name="id">Unique identifier of the message. Required.</param>
    /// <param name="destination">The queue/topic name to publish to. Required.</param>
    /// <param name="message">The message body. Required.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="deduplicationId">
    /// Optional deduplication identifier, interpreted only by transports that support it.
    /// </param>
    /// <param name="attributes">Optional custom attributes; defaults to an empty dictionary.</param>
    /// <exception cref="EmptyArgumentException">
    /// <paramref name="id"/>, <paramref name="destination"/> or <paramref name="message"/> is null, empty or whitespace,
    /// or <paramref name="deduplicationId"/> is supplied but empty or whitespace.
    /// </exception>
    public OutboundMessage(
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

        if (deduplicationId is not null && string.IsNullOrWhiteSpace(deduplicationId))
            throw new EmptyArgumentException(nameof(deduplicationId));

        Id = id;
        Destination = destination;
        Message = message;
        DeduplicationId = deduplicationId;
        PartitionKey = partitionKey;
        Attributes = attributes is null
            ? ReadOnlyDictionary<string, string>.Empty
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(attributes, StringComparer.Ordinal));
    }
}
