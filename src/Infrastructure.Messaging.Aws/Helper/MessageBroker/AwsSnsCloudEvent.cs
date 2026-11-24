using CloudNative.CloudEvents;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker;

/// <summary>An event and its received SNS notification metadata.</summary>
/// <param name="CloudEvent">Structured event.</param>
/// <param name="MessageId">SNS-assigned notification ID.</param>
/// <param name="TopicArn">SNS topic ARN, distinct from the event source.</param>
/// <param name="Message">Structured event body, decompressed when needed.</param>
/// <param name="Attributes">SNS string message attributes.</param>
public sealed record AwsSnsCloudEvent(CloudEvent CloudEvent, string MessageId, string TopicArn, string Message,
                                      IReadOnlyDictionary<string, string> Attributes);
