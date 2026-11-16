using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions;

/// <summary>
/// Thrown when a secret exists in the secret store but has no string value.
/// </summary>
/// <param name="secretName">The name of the secret that returned no value.</param>
public class SecretNotFoundException(string secretName)
    : NotFoundException($"Secret with name: {secretName} returned empty.")
{
    /// <summary>
    /// Gets the name of the secret that returned no value.
    /// </summary>
    public string SecretName { get; } = secretName;
}
