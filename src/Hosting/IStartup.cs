using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Hosting;

/// <summary>
/// Startup contract used by the toolkit's hosts to register services, similar to the ASP.NET Core <c>Startup</c>
/// convention.
/// </summary>
/// <remarks>
/// Implementations must expose a public <c>(IHostEnvironment, IConfiguration)</c> constructor; they are created by
/// <see cref="Anarchy.Toolkit.Hosting.Extension.Microsoft.Hosting.HostBuilderExtension"/>. Most applications derive
/// from <see cref="Anarchy.Toolkit.Hosting.Host.JobStartupBase{TAppSettingsDiModule, TAppSettings}"/>.
/// </remarks>
public interface IStartup
{
    /// <summary>
    /// Registers the application's services.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    void ConfigureServices(IServiceCollection services);
}
