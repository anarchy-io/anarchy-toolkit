using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module;

/// <summary>
/// Registers the <see cref="CorrelationIdOptions"/>, using the defaults unless a factory resolves them from the
/// application's configuration.
/// </summary>
/// <remarks>
/// The middleware itself is not added by this module; call <c>app.UseCorrelationId()</c> on the application builder.
/// </remarks>
/// <param name="options">
/// Optional factory building the options from <see cref="IConfiguration"/>. When <see langword="null"/> (or when it
/// returns <see langword="null"/>), a default <see cref="CorrelationIdOptions"/> is used.
/// </param>
/// <example>
/// <code language="csharp">
/// var module = new ModuleCorrelationId(cfg =&gt; new CorrelationIdOptions
/// {
///     Behavior = CorrelationIdBehavior.IgnoreIfMissing
/// });
/// module.RegisterServices(builder.Services, builder.Configuration, builder.Environment);
/// </code>
/// </example>
public sealed class ModuleCorrelationId(Func<IConfiguration, CorrelationIdOptions>? options = null)
    : IDependencyInjectionModule
{
    /// <inheritdoc/>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services.AddCorrelationId(options?.Invoke(configuration) ?? new CorrelationIdOptions());

        return services;
    }
}
