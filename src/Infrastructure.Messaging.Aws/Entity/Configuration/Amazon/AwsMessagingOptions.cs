namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;

/// <summary>
/// Options that generalize the AWS account/region context required to build
/// destination addresses (topic/queue ARNs) for AWS message-broker publishers.
/// Concrete Sns/Sqs request builders consume this instead of an application
/// specific settings type.
/// </summary>
/// <param name="Region">The AWS region identifier (e.g. "eu-west-1").</param>
/// <param name="AccountId">The AWS account identifier the destinations live in.</param>
public sealed record AwsMessagingOptions(string Region, string AccountId);
