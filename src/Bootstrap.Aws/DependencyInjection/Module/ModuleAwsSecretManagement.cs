using Amazon.KeyManagementService;
using Anarchy.Toolkit.Bootstrap.Aws.Service.AppSecret;
using Anarchy.Toolkit.Bootstrap.Core.Service;
using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Bootstrap.Service.AppSecret;
using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module;

/// <summary>
/// Registers AWS KMS as the <see cref="ISecretManagementService"/> of remote environments.
/// </summary>
/// <remarks>
/// Remotely, registers <see cref="KmsAppSecretService"/> and a default <see cref="AmazonKeyManagementServiceClient"/>
/// (credentials and region from the default AWS SDK chain), replacing any earlier registration. Locally, registers
/// <see cref="InMemoryAppSecretService"/> unless a secret service is already registered. Pass it to
/// <c>ModuleLibConfiguration.WithSecretManagement</c>, or apply it with <c>UseModule</c>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// services.UseModule(
///     new ModuleLibConfiguration&lt;ModuleAppSettings, AppSettings&gt;()
///         .WithSecretManagement(new ModuleAwsSecretManagement()),
///     configuration, environment);
/// </code>
/// </example>
public sealed class ModuleAwsSecretManagement : IDependencyInjectionModule
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        if (environment.IsLocal())
        {
            services.TryAddSingleton<ISecretManagementService, InMemoryAppSecretService>();
            return services;
        }

        return services
            .AddSingleton<ISecretManagementService, KmsAppSecretService>()
            .AddSingleton(_ => new AmazonKeyManagementServiceClient());
    }
}
