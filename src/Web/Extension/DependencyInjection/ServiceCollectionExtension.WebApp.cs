using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace Anarchy.Toolkit.Web.Extension.DependencyInjection;

public static partial class ServiceCollectionExtension
{
    /// <summary>
    /// Registers HTTP response compression with Brotli (<see cref="CompressionLevel.Fastest"/>) and Gzip
    /// (<see cref="CompressionLevel.SmallestSize"/>) providers, enabled for HTTPS.
    /// </summary>
    /// <remarks>
    /// Called by <see cref="Anarchy.Toolkit.Web.Composition.WebAppStartupBase{TAppSettingsDiModule, TAppSettings}.ConfigureServices"/>;
    /// the middleware is added by <c>UseToolkitCommonMiddlewares</c>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddCompressionProviders(this IServiceCollection services)
    {
        return services
            .Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; })
            .Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.SmallestSize; })
            .AddResponseCompression(options => {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
    }
}
