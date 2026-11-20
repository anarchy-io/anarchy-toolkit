using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using System.Collections.ObjectModel;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;

/// <summary>
/// Builds <see cref="OutboundMessage"/> instances for publishing to a message broker.
/// The core API works with already-resolved <c>string</c> destination/source names; generic
/// enum overloads are provided as a convenience and resolve enum values via
/// <see cref="EnumExtension.ToDescription{T}"/>.
/// </summary>
public interface IOutboundMessageBuilder
{
    /// <summary>
    /// Builds an outbound message with an already-resolved destination name.
    /// </summary>
    /// <param name="message">The serialized message body.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom message attributes.</param>
    /// <returns>An outbound message ready for publishing.</returns>
    OutboundMessage Build(
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null);

    /// <summary>
    /// Builds an outbound message overriding the configured destination with an explicit name.
    /// </summary>
    /// <param name="destination">The queue/topic name to publish to.</param>
    /// <param name="message">The serialized message body.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom message attributes.</param>
    /// <returns>An outbound message ready for publishing.</returns>
    OutboundMessage Build(
        string destination,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null);

    /// <summary>
    /// Convenience overload that resolves an enum destination via its description.
    /// </summary>
    /// <typeparam name="TDestination">The destination enum type.</typeparam>
    /// <param name="destination">The destination; its description is used as the queue/topic name.</param>
    /// <param name="message">The serialized message body.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom message attributes.</param>
    /// <returns>An outbound message ready for publishing.</returns>
    OutboundMessage Build<TDestination>(
        TDestination destination,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        where TDestination : Enum;
}

/// <summary>
/// Default <see cref="IOutboundMessageBuilder"/> bound to a default destination and an optional source.
/// </summary>
/// <remarks>
/// Every built message gets a new random GUID as both <see cref="OutboundMessage.Id"/> and
/// <see cref="OutboundMessage.DeduplicationId"/>. When <paramref name="source"/> is not empty it is added as
/// the <c>Source</c> attribute, overriding any caller-supplied <c>Source</c> attribute. The builder is immutable and
/// thread-safe.
/// </remarks>
/// <param name="destination">The default queue/topic name used by <see cref="Build(string, string?, IReadOnlyDictionary{string, string}?)"/>.</param>
/// <param name="source">Optional name of the publishing application, added as the <c>Source</c> attribute.</param>
/// <example>
/// <code language="csharp">
/// var builder = new OutboundMessageBuilder("orders-events.fifo", source: "order-service");
/// var outbound = builder.Build(domainEvent.ToJson(), partitionKey: domainEvent.AggregateId);
/// await publisher.PublishAsync(outbound);
/// </code>
/// </example>
public class OutboundMessageBuilder(string destination, string? source = null)
    : IOutboundMessageBuilder
{
    /// <summary>
    /// Convenience constructor that resolves an enum destination/source via their descriptions.
    /// </summary>
    /// <typeparam name="TDestination">The destination enum type.</typeparam>
    /// <param name="destination">The default destination; its description is used as the queue/topic name.</param>
    /// <param name="source">Optional name of the publishing application.</param>
    /// <returns>A new builder.</returns>
    public static OutboundMessageBuilder Create<TDestination>(
        TDestination destination,
        string? source = null)
        where TDestination : Enum
        => new(destination.ToDescription(), source);

    /// <summary>
    /// Convenience constructor that resolves both enum destination and enum source via their descriptions.
    /// </summary>
    /// <typeparam name="TDestination">The destination enum type.</typeparam>
    /// <typeparam name="TSource">The source enum type.</typeparam>
    /// <param name="destination">The default destination; its description is used as the queue/topic name.</param>
    /// <param name="source">The publishing application; its description is used as the <c>Source</c> attribute.</param>
    /// <returns>A new builder.</returns>
    public static OutboundMessageBuilder Create<TDestination, TSource>(
        TDestination destination,
        TSource source)
        where TDestination : Enum
        where TSource : Enum
        => new(destination.ToDescription(), source.ToDescription());

    /// <inheritdoc/>
    public OutboundMessage Build(
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        => Build(destination, message, partitionKey, attributes);

    /// <summary>
    /// Builds an outbound message overriding the configured destination with an explicit name.
    /// </summary>
    /// <param name="destinationName">The queue/topic name to publish to.</param>
    /// <param name="message">The serialized message body.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom message attributes.</param>
    /// <returns>An outbound message ready for publishing.</returns>
    /// <exception cref="Core.Exceptions.EmptyArgumentException">
    /// <paramref name="destinationName"/> or <paramref name="message"/> is null, empty or whitespace.
    /// </exception>
    public OutboundMessage Build(
        string destinationName,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        return new OutboundMessage(
            Guid.NewGuid().ToString(),
            destinationName,
            message,
            partitionKey,
            Guid.NewGuid().ToString(),
            BuildAttributes(attributes));
    }

    /// <summary>
    /// Convenience overload that resolves an enum destination via its description.
    /// </summary>
    /// <typeparam name="TDestination">The destination enum type.</typeparam>
    /// <param name="destinationValue">The destination; its description is used as the queue/topic name.</param>
    /// <param name="message">The serialized message body.</param>
    /// <param name="partitionKey">Optional partition/message-group key.</param>
    /// <param name="attributes">Optional custom message attributes.</param>
    /// <returns>An outbound message ready for publishing.</returns>
    public OutboundMessage Build<TDestination>(
        TDestination destinationValue,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        where TDestination : Enum
        => Build(destinationValue.ToDescription(), message, partitionKey, attributes);

    /// <summary>
    /// Merges the configured source (when present) into the provided attribute set.
    /// Callers are responsible for supplying any additional well-known attributes
    /// (correlation id, aggregate type/id, event type, etc.) via <paramref name="attributes"/>.
    /// </summary>
    private IReadOnlyDictionary<string, string> BuildAttributes(
        IReadOnlyDictionary<string, string>? attributes)
    {
        var result = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes);

        if (!string.IsNullOrWhiteSpace(source))
        {
            result["Source"] = source;
        }

        return new ReadOnlyDictionary<string, string>(result);
    }
}
