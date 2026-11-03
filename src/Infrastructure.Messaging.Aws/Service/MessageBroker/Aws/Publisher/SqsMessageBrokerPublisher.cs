using Amazon.SQS;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

public class SqsMessageBrokerPublisher(
    IAmazonSQS clientSqs,
    IAwsPublisherRequestBuilder<SendMessageRequest, SendMessageBatchRequest> builderPublisherRequest,
    Observer observer)
    : AwsMessageBrokerPublisherBase<SendMessageRequest, SendMessageBatchRequest>(builderPublisherRequest, observer)
{
    protected override async Task SendRequestAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        await clientSqs.SendMessageAsync(request, cancellationToken);
    }

    protected override async Task SendBatchRequestAsync(SendMessageBatchRequest batchRequest,
                                                        CancellationToken cancellationToken)
    {
        await clientSqs.SendMessageBatchAsync(batchRequest, cancellationToken);
    }
}
