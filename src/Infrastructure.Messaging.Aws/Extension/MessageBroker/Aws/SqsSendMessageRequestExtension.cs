using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;

public static class SqsSendMessageRequestExtensions
{
    private const int SqsMaxSecondsDelay = 900; // 15 minutes
    private const string NotGoodBeforeTimestamp = "NotGoodBeforeTimestamp";

    extension(SendMessageRequest request)
    {
        public void SetDestination(string queueUrl)
        {
            request.QueueUrl = queueUrl;
        }

        public void SetMessage(string body)
        {
            request.MessageBody = body;
        }

        public void SetFifoInfo(string? groupId, string? deduplicationId)
        {
            request.MessageGroupId = groupId;
            request.MessageDeduplicationId = deduplicationId;
        }

        public void SetAttributes(Dictionary<string, MessageAttributeValue> attributes)
        {
            request.MessageAttributes = attributes;

            if (!attributes.TryGetValue(NotGoodBeforeTimestamp, out var timestamp) ||
                !DateTimeOffset.TryParse(timestamp.StringValue, out var timestampValue))
            {
                return;
            }

            var delay = Convert.ToInt32(timestampValue.Subtract(DateHelper.Now()).TotalSeconds);

            if (delay > 0)
            {
                request.DelaySeconds = int.Min(delay, SqsMaxSecondsDelay);
            }
        }
    }
}
