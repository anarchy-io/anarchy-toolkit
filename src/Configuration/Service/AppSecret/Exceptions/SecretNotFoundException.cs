using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions;

/// <summary>
/// Thrown when a secret exists in the secret store but has no string value.
/// </summary>
public class SecretNotFoundException(string secretName)
    : NotFoundException($"Secret with name: {secretName} returned empty.")
{
    public string SecretName { get; } = secretName;
}
