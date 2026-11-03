using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

public class SnsMessageBrokerPublisher(
    IAmazonSimpleNotificationService clientSns,
    IAwsPublisherRequestBuilder<PublishRequest, PublishBatchRequest> builderPublisherRequest,
    Observer observer)
    : AwsMessageBrokerPublisherBase<PublishRequest, PublishBatchRequest>(builderPublisherRequest, observer)
{
    protected override async Task SendRequestAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        await clientSns.PublishAsync(request, cancellationToken);
    }

    protected override async Task SendBatchRequestAsync(PublishBatchRequest batchRequest,
                                                        CancellationToken cancellationToken)
    {
        await clientSns.PublishBatchAsync(batchRequest, cancellationToken);
    }
}
