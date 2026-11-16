using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions;

/// <summary>
/// Thrown when the secret store fails to return a secret.
/// </summary>
/// <param name="secretName">The name of the secret that could not be retrieved.</param>
/// <param name="innerException">The underlying error.</param>
public class SecretRetrievalException(string secretName, Exception innerException)
    : ExternalDependencyException(
        $"Error getting secret with name: {secretName} returned: {innerException.Message}", innerException)
{
    /// <summary>
    /// Gets the name of the secret that could not be retrieved.
    /// </summary>
    public string SecretName { get; } = secretName;
}
