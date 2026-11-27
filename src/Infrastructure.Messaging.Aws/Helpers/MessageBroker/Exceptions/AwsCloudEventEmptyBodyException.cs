using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helpers.MessageBroker.Exceptions;

/// <summary>Thrown when the AWS request builder produces an empty body for a structured CloudEvent.</summary>
/// <param name="transport">The AWS transport whose request has an empty body.</param>
public class AwsCloudEventEmptyBodyException(string transport)
    : InvalidArgumentException($"The {transport} CloudEvent request has an empty body, which AWS does not accept. " +
                               "The configured builder may have compressed an oversized message into attributes.")
{
    /// <summary>The AWS transport whose request has an empty body.</summary>
    public string Transport { get; } = transport;
}
