using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

/// <summary>
/// Translates toolkit <see cref="OutboundMessage"/> instances into AWS SDK request objects.
/// </summary>
/// <remarks>
/// Implemented by
/// <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.SnsAwsPublisherRequestBuilder"/>
/// and
/// <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.SqsAwsPublisherRequestBuilder"/>
/// and consumed by <see cref="AwsMessageBrokerPublisherBase{TRequest, TBatchRequest}"/>.
/// </remarks>
/// <typeparam name="TRequest">The AWS single-message request type (e.g. SNS <c>PublishRequest</c>).</typeparam>
/// <typeparam name="TBatchRequest">The AWS batch request type (e.g. SNS <c>PublishBatchRequest</c>).</typeparam>
public interface IAwsPublisherRequestBuilder<out TRequest, out TBatchRequest>
{
    /// <summary>
    /// Builds a single-message request from <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <returns>The AWS request.</returns>
    TRequest FromOutboundMessage(OutboundMessage message);

    /// <summary>
    /// Builds a batch request from <paramref name="messages"/>. All messages are expected to share the same
    /// destination and the batch must respect the AWS batch limit (10 entries).
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <returns>The AWS batch request.</returns>
    TBatchRequest FromOutboundMessage(OutboundMessage[] messages);
}
