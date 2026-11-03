using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Anarchy.Toolkit.Configuration.Service.AppSecret.Exceptions;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret;

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
