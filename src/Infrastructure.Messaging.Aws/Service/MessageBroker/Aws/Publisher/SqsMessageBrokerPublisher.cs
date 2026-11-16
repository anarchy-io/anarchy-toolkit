using Amazon.SQS;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

/// <summary>
/// AWS SQS implementation of the message-broker publisher. Registered by <c>AddSqsMessageBroker</c> and exposed as the
/// keyed (<c>"queue"</c>) <c>IMessageBrokerPublisher</c>.
/// </summary>
/// <param name="clientSqs">The SQS client.</param>
/// <param name="builderPublisherRequest">Builds SQS requests from outbound messages.</param>
/// <param name="observer">The observer used for logging and metrics.</param>
public class SqsMessageBrokerPublisher(
    IAmazonSQS clientSqs,
    IAwsPublisherRequestBuilder<SendMessageRequest, SendMessageBatchRequest> builderPublisherRequest,
    Observer observer)
    : AwsMessageBrokerPublisherBase<SendMessageRequest, SendMessageBatchRequest>(builderPublisherRequest, observer)
{
    /// <inheritdoc/>
    protected override async Task SendRequestAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        await clientSqs.SendMessageAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task SendBatchRequestAsync(SendMessageBatchRequest batchRequest,
                                                        CancellationToken cancellationToken)
    {
        await clientSqs.SendMessageBatchAsync(batchRequest, cancellationToken);
    }
}
