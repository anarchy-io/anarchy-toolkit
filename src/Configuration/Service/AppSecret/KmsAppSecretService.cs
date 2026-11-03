using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Anarchy.Toolkit.Configuration.Core.Service;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret;

public sealed class KmsAppSecretService(AmazonKeyManagementServiceClient kmsClient) : ISecretManagementService
{
    public async Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(value));

        var decryptRequest = new DecryptRequest { CiphertextBlob = stream };

        var response = await kmsClient.DecryptAsync(decryptRequest, cancellationToken ?? CancellationToken.None);

        using var reader = new StreamReader(response.Plaintext);
        return await reader.ReadToEndAsync();
    }
}
