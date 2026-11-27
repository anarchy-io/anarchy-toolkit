using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Anarchy.Toolkit.Bootstrap.Core.Service;

namespace Anarchy.Toolkit.Bootstrap.Aws.Service.AppSecret;

/// <summary>
/// <see cref="ISecretManagementService"/> that decrypts Base64-encoded ciphertext with AWS KMS. Used in remote
/// environments.
/// </summary>
/// <param name="kmsClient">The AWS KMS client.</param>
public sealed class KmsAppSecretService(AmazonKeyManagementServiceClient kmsClient) : ISecretManagementService
{
    /// <summary>
    /// Decrypts a Base64-encoded KMS ciphertext and returns the plaintext read as text.
    /// </summary>
    /// <param name="value">The Base64-encoded ciphertext blob.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The decrypted plain-text value.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="value"/> is not valid Base64.</exception>
    public async Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(value));

        var decryptRequest = new DecryptRequest { CiphertextBlob = stream };

        var response = await kmsClient.DecryptAsync(decryptRequest, cancellationToken ?? CancellationToken.None);

        using var reader = new StreamReader(response.Plaintext);
        return await reader.ReadToEndAsync();
    }
}
