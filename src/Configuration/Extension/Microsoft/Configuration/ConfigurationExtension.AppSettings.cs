using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;

/// <summary>
/// Extension methods for <see cref="IConfiguration"/> related to application settings.
/// </summary>
public static class ConfigurationExtension
{
    /// <summary>
    /// Binds the configuration to a new instance of <typeparamref name="TSettings"/> and validates it using
    /// data annotations. Consumers implementing <see cref="IAppSettings"/> can bind their own settings type.
    /// </summary>
    /// <remarks>
    /// Useful when settings are needed before the DI container is built; inside DI prefer the options registered by
    /// <see cref="Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity.ModuleAppSettingsBase{TAppSettings}"/>.
    /// Validation covers all properties of the root object (nested objects are not validated recursively).
    /// </remarks>
    /// <typeparam name="TSettings">The settings type to bind and validate. Must have a public parameterless constructor.</typeparam>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>A validated instance of <typeparamref name="TSettings"/>.</returns>
    /// <exception cref="ValidationException">Thrown if the bound settings fail data-annotation validation.</exception>
    /// <example>
    /// <code language="csharp">
    /// var settings = configuration.BuildValidAppSettings&lt;AppSettings&gt;();
    /// </code>
    /// </example>
    public static TSettings BuildValidAppSettings<TSettings>(this IConfiguration configuration)
        where TSettings : class, IAppSettings, new()
    {
        var result = new TSettings();
        configuration.Bind(result);
        Validator.ValidateObject(result, new ValidationContext(result), validateAllProperties: true);

        return result;
    }
}
