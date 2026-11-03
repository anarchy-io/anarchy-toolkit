using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using System.Collections.ObjectModel;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;

/// <summary>
/// Builds <see cref="OutboundMessageEntity"/> instances for publishing to a message broker.
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
    /// <returns>An outbound message entity ready for publishing.</returns>
    OutboundMessageEntity Build(
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null);

    /// <summary>
    /// Builds an outbound message overriding the configured destination with an explicit name.
    /// </summary>
    OutboundMessageEntity Build(
        string destination,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null);

    /// <summary>
    /// Convenience overload that resolves an enum destination via its description.
    /// </summary>
    OutboundMessageEntity Build<TDestination>(
        TDestination destination,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        where TDestination : Enum;
}

public class OutboundMessageBuilder(string destination, string? source = null)
    : IOutboundMessageBuilder
{
    /// <summary>
    /// Convenience constructor that resolves an enum destination/source via their descriptions.
    /// </summary>
    public static OutboundMessageBuilder Create<TDestination>(
        TDestination destination,
        string? source = null)
        where TDestination : Enum
        => new(destination.ToDescription(), source);

    /// <summary>
    /// Convenience constructor that resolves both enum destination and enum source via their descriptions.
    /// </summary>
    public static OutboundMessageBuilder Create<TDestination, TSource>(
        TDestination destination,
        TSource source)
        where TDestination : Enum
        where TSource : Enum
        => new(destination.ToDescription(), source.ToDescription());

    public OutboundMessageEntity Build(
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
        => Build(destination, message, partitionKey, attributes);

    public OutboundMessageEntity Build(
        string destinationName,
        string message,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        return new OutboundMessageEntity(
            Guid.NewGuid().ToString(),
            destinationName,
            message,
            partitionKey,
            Guid.NewGuid().ToString(),
            BuildAttributes(attributes));
    }

    public OutboundMessageEntity Build<TDestination>(
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
