using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker.Exceptions;

/// <summary>Thrown when JSON in an AWS CloudEvent message or SNS notification cannot be parsed.</summary>
/// <param name="representation">The representation being parsed, without its potentially sensitive contents.</param>
/// <param name="innerException">The underlying JSON parser failure.</param>
public class AwsCloudEventJsonParsingException(string representation, Exception innerException)
    : JsonParsingException($"Unable to parse {representation} JSON in an AWS CloudEvent message.", innerException)
{
    /// <summary>The representation that could not be parsed.</summary>
    public string Representation { get; } = representation;
}
