using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes.Exceptions;

/// <summary>Thrown when a CloudEvent cannot be mapped to or from an Anarchy envelope or message.</summary>
public class InvalidCloudEventMappingException(string attribute, Exception? innerException = null)
    : InvalidArgumentException($"CloudEvent attribute or data '{attribute}' is missing, invalid or colliding for the Anarchy mapping.",
                               innerException)
{
    /// <summary>The attribute or data that cannot be mapped.</summary>
    public string Attribute { get; } = attribute;
}
