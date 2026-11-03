using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace Anarchy.Toolkit.Web.Extension.DependencyInjection;

public static partial class ServiceCollectionExtension
{
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
