using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

/// <summary>
/// AWS SNS implementation of the message-broker publisher. Registered by <c>AddSnsMessageBroker</c> and exposed as the
/// keyed (<c>"topic"</c>) <c>IMessageBrokerPublisher</c>.
/// </summary>
/// <param name="clientSns">The SNS client.</param>
/// <param name="builderPublisherRequest">Builds SNS requests from outbound messages.</param>
/// <param name="observer">The observer used for logging and metrics.</param>
public class SnsMessageBrokerPublisher(
    IAmazonSimpleNotificationService clientSns,
    IAwsPublisherRequestBuilder<PublishRequest, PublishBatchRequest> builderPublisherRequest,
    Observer observer)
    : AwsMessageBrokerPublisherBase<PublishRequest, PublishBatchRequest>(builderPublisherRequest, observer)
{
    /// <inheritdoc/>
    protected override async Task SendRequestAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        await clientSns.PublishAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task SendBatchRequestAsync(PublishBatchRequest batchRequest,
                                                        CancellationToken cancellationToken)
    {
        await clientSns.PublishBatchAsync(batchRequest, cancellationToken);
    }
}
