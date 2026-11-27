using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker;

/// <summary>An SNS notification received through SQS, with both sets of broker metadata.</summary>
/// <param name="Notification">SNS event and notification metadata.</param>
/// <param name="Message">SQS message and its metadata.</param>
public sealed record AwsSnsSqsCloudEvent(AwsSnsCloudEvent Notification, InboundMessage Message);
