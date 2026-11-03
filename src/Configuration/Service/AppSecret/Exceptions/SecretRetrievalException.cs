using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions;

/// <summary>
/// Thrown when the secret store fails to return a secret.
/// </summary>
public class SecretRetrievalException(string secretName, Exception innerException)
    : ExternalDependencyException(
        $"Error getting secret with name: {secretName} returned: {innerException.Message}", innerException)
{
    public string SecretName { get; } = secretName;
}
