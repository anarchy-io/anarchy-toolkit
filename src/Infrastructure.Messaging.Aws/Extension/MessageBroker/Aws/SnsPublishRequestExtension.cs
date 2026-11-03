using Amazon.SimpleNotificationService.Model;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;

public static class SnsPublishRequestExtension
{
    extension(PublishRequest request)
    {
        public void SetDestination(string topicArn)
        {
            request.TopicArn = topicArn;
        }

        public void SetMessage(string body)
        {
            request.Message = body;
        }

        public void SetFifoInfo(string? groupId, string? deduplicationId)
        {
            request.MessageGroupId = groupId;
            request.MessageDeduplicationId = deduplicationId;
        }

        public void SetAttributes(Dictionary<string, MessageAttributeValue> attributes)
        {
            request.MessageAttributes = attributes;
        }
    }
}
