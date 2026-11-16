using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Helper;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;

/// <summary>
/// Setters for SQS <see cref="SendMessageRequest"/> used by
/// <see cref="Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.SqsAwsPublisherRequestBuilder"/>.
/// </summary>
public static class SqsSendMessageRequestExtensions
{
    private const int SqsMaxSecondsDelay = 900; // 15 minutes
    private const string NotGoodBeforeTimestamp = "NotGoodBeforeTimestamp";

    /// <param name="request">The SQS send request to modify.</param>
    extension(SendMessageRequest request)
    {
        /// <summary>Sets <see cref="SendMessageRequest.QueueUrl"/>.</summary>
        /// <param name="queueUrl">The URL of the destination queue.</param>
        public void SetDestination(string queueUrl)
        {
            request.QueueUrl = queueUrl;
        }

        /// <summary>Sets <see cref="SendMessageRequest.MessageBody"/>.</summary>
        /// <param name="body">The message body.</param>
        public void SetMessage(string body)
        {
            request.MessageBody = body;
        }

        /// <summary>Sets the FIFO message group id and deduplication id.</summary>
        /// <param name="groupId">The message group id, or <see langword="null"/>.</param>
        /// <param name="deduplicationId">The deduplication id, or <see langword="null"/>.</param>
        public void SetFifoInfo(string? groupId, string? deduplicationId)
        {
            request.MessageGroupId = groupId;
            request.MessageDeduplicationId = deduplicationId;
        }

        /// <summary>
        /// Replaces <see cref="SendMessageRequest.MessageAttributes"/> with <paramref name="attributes"/> and derives a
        /// delivery delay from the <c>NotGoodBeforeTimestamp</c> attribute when present.
        /// </summary>
        /// <remarks>
        /// When the attribute contains a parseable <see cref="DateTimeOffset"/> in the future,
        /// <see cref="SendMessageRequest.DelaySeconds"/> is set to the number of seconds until then, capped at the SQS
        /// maximum of 900 seconds (15 minutes). Past or unparseable timestamps are ignored.
        /// </remarks>
        /// <param name="attributes">The message attributes.</param>
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
