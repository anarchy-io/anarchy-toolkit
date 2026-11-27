using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using CloudNative.CloudEvents;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker;

/// <summary>An event and the separate SQS message from which it was received.</summary>
/// <param name="CloudEvent">Structured event.</param>
/// <param name="Message">SQS broker metadata and body as returned by the inbound mapper, which decompresses when needed.</param>
public sealed record AwsReceivedCloudEvent(CloudEvent CloudEvent, InboundMessage Message);
