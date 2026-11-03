using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Composition.Module;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;

/// <summary>
/// Dependency-injection module that binds and registers the consumer's <typeparamref name="TAppSettings"/>.
/// </summary>
/// <typeparam name="TAppSettings">The consumer's concrete application settings type.</typeparam>
public interface IAppSettingsDiModule<TAppSettings> : IDependencyInjectionModule
    where TAppSettings : class, IAppSettings;
