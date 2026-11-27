using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Entity;

/// <summary>
/// Default <see cref="IAppSettingsDiModule{TAppSettings}"/>: binds <typeparamref name="TAppSettings"/> from the root
/// configuration, validates it with data annotations on start, and registers it as a singleton.
/// </summary>
/// <remarks>
/// Registers <c>IOptions&lt;TAppSettings&gt;</c>, <typeparamref name="TAppSettings"/> and
/// <see cref="IAppSettings"/>. Derive from it and override <see cref="RegisterAdditionalTypesForSettings"/> to also
/// expose capability interfaces such as
/// <c>IAwsAppSettings</c>.
/// </remarks>
/// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
/// <example>
/// <code language="csharp">
/// public sealed class ModuleAppSettings : ModuleAppSettingsBase&lt;AppSettings&gt;
/// {
///     protected override IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services) =>
///         services.AddSingleton&lt;IAwsAppSettings&gt;(sp => sp.GetRequiredService&lt;AppSettings&gt;());
/// }
/// </code>
/// </example>
public class ModuleAppSettingsBase<TAppSettings> : IAppSettingsDiModule<TAppSettings>
    where TAppSettings : class, IAppSettings
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment _)
    {
        services
            .AddOptions<TAppSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Consumers depend on the capability interfaces (IAwsAppSettings, IPostgresAppSettings, ...) rather
        // than on the concrete AppSettings, so each section is resolvable on its own.
        var result = services
            .AddSingleton<TAppSettings>(sp => sp.GetRequiredService<IOptions<TAppSettings>>().Value)
            .AddSingleton<IAppSettings>(sp => sp.GetRequiredService<TAppSettings>())
            ;

        return RegisterAdditionalTypesForSettings(result);
    }

    /// <summary>
    /// Registers additional types for the specified AppSettings, for example capability interfaces (IAwsAppSettings,
    /// IPostgresAppSettings, ...) that are implemented by the TAppSettings.
    ///
    /// </summary>
    /// <param name="services">The service collection to register the types with.</param>
    /// <returns>The updated service collection.</returns>
    protected virtual IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services)
    {
        return services;
    }
}
