using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;

public static class ConfigurationExtension
{
    /// <summary>
    /// Binds the configuration to a new instance of <typeparamref name="TSettings"/> and validates it using
    /// data annotations. Consumers implementing <see cref="IAppSettings"/> can bind their own settings type.
    /// </summary>
    /// <typeparam name="TSettings">The settings type to bind and validate. Must have a public parameterless constructor.</typeparam>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>A validated instance of <typeparamref name="TSettings"/>.</returns>
    public static TSettings BuildValidAppSettings<TSettings>(this IConfiguration configuration)
        where TSettings : class, IAppSettings, new()
    {
        var result = new TSettings();
        configuration.Bind(result);
        Validator.ValidateObject(result, new ValidationContext(result), validateAllProperties: true);

        return result;
    }
}
