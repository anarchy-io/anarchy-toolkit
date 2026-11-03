using Amazon.KeyManagementService;
using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Configuration.Service.AppSecret;
using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;

internal sealed class ModuleSecretManagement : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return environment.IsLocal() ?
            services.AddSingleton<ISecretManagementService, InMemoryAppSecretService>() :
            services
                .AddSingleton<ISecretManagementService, KmsAppSecretService>()
                .AddSingleton(new AmazonKeyManagementServiceClient());
    }
}
