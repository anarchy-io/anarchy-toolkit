namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;

/// <summary>Thrown when a required Kafka API argument is null.</summary>
/// <param name="paramName">The name of the null argument.</param>
public class KafkaNullArgumentException(string paramName)
    : ArgumentNullException(paramName, "A required Kafka argument cannot be null.");
