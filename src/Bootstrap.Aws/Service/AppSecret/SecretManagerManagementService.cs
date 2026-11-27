using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Anarchy.Toolkit.Bootstrap.Aws.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Bootstrap.Service.AppSecret.Exceptions;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Bootstrap.Aws.Service.AppSecret;

/// <summary>
/// Process-wide reader of AWS Secrets Manager secrets with an in-memory cache.
/// </summary>
/// <remarks>
/// Uses a lazily created singleton with a default <c>AmazonSecretsManagerClient</c> (credentials and region from the
/// default AWS SDK chain). Secrets are cached for the lifetime of the process without expiration. Typically accessed
/// through <c>IHostEnvironment.GetAwsSecretValue</c> in
/// <see cref="AwsHostEnvironmentExtensions"/>.
/// </remarks>
public class SecretManagerManagementService
{
    private static readonly Lazy<SecretManagerManagementService> Instance =
        new(() => new SecretManagerManagementService());

    private readonly ConcurrentDictionary<string, string> _secretCache = new();
    private readonly AmazonSecretsManagerClient _secretManagerClient;

    private SecretManagerManagementService()
    {
        _secretManagerClient = new AmazonSecretsManagerClient();
    }

    /// <summary>
    /// Gets the string value of a secret, from the cache when available.
    /// </summary>
    /// <remarks>Blocks synchronously on the asynchronous AWS call; intended for startup/configuration time.</remarks>
    /// <param name="secretName">The secret name or ARN.</param>
    /// <returns>The secret string value.</returns>
    /// <exception cref="SecretNotFoundException">Thrown if the secret has no string value.</exception>
    /// <exception cref="SecretRetrievalException">Thrown if retrieving the secret fails for any other reason.</exception>
    public static string GetSecretWithCache(string secretName)
    {
        return Instance.Value.GetSecretFromCacheAsync(secretName)
            .GetAwaiter()
            .GetResult();
    }

    private async Task<string> GetSecretFromCacheAsync(string secretName,
                                                      CancellationToken? cancellationToken = null)
    {
        try
        {
            if (_secretCache.TryGetValue(secretName, out var cachedSecret))
            {
                return cachedSecret;
            }

            var secretResponse = await _secretManagerClient.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = secretName },
                cancellationToken ?? CancellationToken.None
            );

            var secretValue = secretResponse.SecretString;

            if (secretValue == null)
            {
                throw new SecretNotFoundException(secretName);
            }

            _secretCache.TryAdd(secretName, secretValue);

            return secretValue;
        }
        catch (Exception e) when (e is not SecretNotFoundException)
        {
            throw new SecretRetrievalException(secretName, e);
        }
    }
}
