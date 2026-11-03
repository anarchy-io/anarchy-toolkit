using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Service.AppSecret;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;

public static class HostEnvironmentEnvExtension
{
    extension(IHostEnvironment hostEnvironment)
    {
        public bool IsLocal()
        {
            return hostEnvironment.IsDevelopment() || !hostEnvironment.IsTAP();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool IsTAP()
        {
            return hostEnvironment.IsTesting() || hostEnvironment.IsAcceptance() || hostEnvironment.IsProduction();
        }

        public bool IsTesting()
        {
            return hostEnvironment.IsEnvironment(AppEnvironments.Testing);
        }

        public bool IsAcceptance()
        {
            return hostEnvironment.IsEnvironment(AppEnvironments.Acceptance);
        }

        public string GetSecretValue(string secretName)
        {
            return hostEnvironment.IsLocal() ? secretName : SecretManagerManagementService.GetSecretWithCache(secretName);
        }
    }
}
