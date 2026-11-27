using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Core.Messaging.Abstractions.Entity.MessageBroker;

/// <summary>Correlation scopes for messages received from AWS brokers.</summary>
public static class InboundMessageCorrelationExtensions
{
    /// <summary>
    /// Opens a provider-neutral correlation scope from the message's string attributes. Use it around message handling
    /// so the identifiers flow through async work, logger scopes and the current activity.
    /// </summary>
    /// <param name="message">The mapped inbound message.</param>
    /// <param name="logger">The logger whose provider receives the structured scope.</param>
    /// <param name="correlationIdAttributeName">The attribute carrying the correlation id.</param>
    /// <param name="attemptIdAttributeName">The attribute carrying the attempt id.</param>
    /// <returns>A scope that restores the prior correlation context when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="logger"/> is null.</exception>
    /// <example>
    /// <code language="csharp">
    /// var messages = InboundMessageMapper.FromSqs(sqsEvent);
    /// foreach (var message in messages)
    /// {
    ///     using (message.BeginCorrelationScope(logger))
    ///         await processor.HandleAsync(message);
    /// }
    /// </code>
    /// </example>
    public static IDisposable BeginCorrelationScope(
        this InboundMessage message,
        ILogger logger,
        string correlationIdAttributeName = CorrelationLogPropertyNames.CorrelationId,
        string attemptIdAttributeName = CorrelationLogPropertyNames.AttemptId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(logger);

        message.Attributes.TryGetValue(correlationIdAttributeName, out var correlationId);
        message.Attributes.TryGetValue(attemptIdAttributeName, out var attemptId);

        return CorrelationContext.BeginScope(logger, correlationId, attemptId,
            correlationIdAttributeName, attemptIdAttributeName);
    }
}
