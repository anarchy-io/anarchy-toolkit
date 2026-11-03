using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

public interface IAwsPublisherRequestBuilder<out TRequest, out TBatchRequest>
{
    TRequest FromOutboundMessage(OutboundMessageEntity message);
    TBatchRequest FromOutboundMessage(OutboundMessageEntity[] messages);
}
