using Amazon.SimpleNotificationService.Model;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Amazon.SQS.Model;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Amazon.SimpleNotificationService.Model;

/// <summary>
/// Setters for SNS <see cref="PublishRequest"/> used by
/// <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.SnsAwsPublisherRequestBuilder"/>,
/// mirroring <see cref="SqsSendMessageRequestExtensions"/> so both builders share the same shape.
/// </summary>
public static class SnsPublishRequestExtensions
{
    /// <param name="request">The SNS publish request to modify.</param>
    extension(PublishRequest request)
    {
        /// <summary>Sets <see cref="PublishRequest.TopicArn"/>.</summary>
        /// <param name="topicArn">The ARN of the destination topic.</param>
        public void SetDestination(string topicArn)
        {
            request.TopicArn = topicArn;
        }

        /// <summary>Sets <see cref="PublishRequest.Message"/>.</summary>
        /// <param name="body">The message body.</param>
        public void SetMessage(string body)
        {
            request.Message = body;
        }

        /// <summary>Sets the FIFO message group id and deduplication id.</summary>
        /// <param name="groupId">The message group id, or <see langword="null"/>.</param>
        /// <param name="deduplicationId">The deduplication id, or <see langword="null"/>.</param>
        public void SetFifoInfo(string? groupId, string? deduplicationId)
        {
            request.MessageGroupId = groupId;
            request.MessageDeduplicationId = deduplicationId;
        }

        /// <summary>Replaces <see cref="PublishRequest.MessageAttributes"/> with <paramref name="attributes"/>.</summary>
        /// <param name="attributes">The message attributes.</param>
        public void SetAttributes(Dictionary<string, MessageAttributeValue> attributes)
        {
            request.MessageAttributes = attributes;
        }
    }
}
