using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    /// <summary>
    /// Utility functions for interacting with Amazon KMS.
    /// </summary>
    public static class Kms
    {
        /// <summary>
        /// Decrypts an encrypted secret using KMS.
        /// </summary>
        public static string DecryptSecret(this IDefaultBuild build, Secret secret) =>
            Task.Run(
                     async () => {
                         var decryptRequest = new DecryptRequest {
                             CiphertextBlob = new MemoryStream(Convert.FromBase64String(secret.Value)),
                         };
                         var kms = _kmsInstances.GetOrAdd(build, CreateKms);
                         var response = await kms.DecryptAsync(decryptRequest);

                         using var reader = new StreamReader(response.Plaintext);
                         return await reader.ReadToEndAsync();
                     }
                 )
                .GetAwaiter().GetResult();

        private static IAmazonKeyManagementService CreateKms(IDefaultBuild build)
        {
            var options = new AWSOptions {
                Profile = build.AwsProfile,
                Region = build.AwsRegion != null
                                           ? RegionEndpoint.GetBySystemName(build.AwsRegion)
                                           : null,
            };
            return options.CreateServiceClient<IAmazonKeyManagementService>();
        }

        private static readonly ConcurrentDictionary<IDefaultBuild, IAmazonKeyManagementService> _kmsInstances =
            new ConcurrentDictionary<IDefaultBuild, IAmazonKeyManagementService>();
    }
}
