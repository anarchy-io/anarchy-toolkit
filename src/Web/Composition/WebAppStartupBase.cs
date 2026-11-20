using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Hosting.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Http.Common;
using Anarchy.Toolkit.Web.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Presentation.Converter.Json;
using Anarchy.Toolkit.Web.Service.Worker;

namespace Anarchy.Toolkit.Web.Composition;

/// <summary>
/// Base class for ASP.NET Core startups of toolkit web applications. Builds the dependency injection container from
/// the consumer's application settings module and registers controllers, JSON serialization and response compression.
/// </summary>
/// <remarks>
/// <para>
/// Instances are created by the host (<see cref="Anarchy.Toolkit.Web.WebAppEntryPoint"/> or
/// <see cref="Anarchy.Toolkit.Web.AwsWebAppEntryPointBase{T}"/>), which calls <see cref="ConfigureServices"/> and then
/// <see cref="Configure"/>. Derived classes must expose a constructor taking
/// (<see cref="IWebHostEnvironment"/>, <see cref="IConfiguration"/>).
/// </para>
/// <para>
/// <see cref="ConfigureServices"/> runs <c>DiFactory&lt;TAppSettingsDiModule, TAppSettings&gt;.Build</c>, passes the
/// result to <see cref="ConfigureWebCustomServices"/>, then adds compression and controllers with Newtonsoft
/// (multi-case contract resolver) and System.Text.Json (no naming policy plus the toolkit converters in
/// <c>Anarchy.Toolkit.Web.Presentation.Converter.Json</c>).
/// </para>
/// <para>
/// Most REST APIs should derive from
/// <see cref="Anarchy.Toolkit.Web.Composition.Startup.RestApiWebAppStartupBase{TAppSettingsDiModule, TAppSettings}"/> instead.
/// </para>
/// </remarks>
/// <typeparam name="TAppSettingsDiModule">The DI module that binds and registers <typeparamref name="TAppSettings"/>.</typeparam>
/// <typeparam name="TAppSettings">The application settings type.</typeparam>
/// <param name="environment">The web hosting environment.</param>
/// <param name="configuration">The application configuration.</param>
public abstract class WebAppStartupBase<TAppSettingsDiModule, TAppSettings>(
    IWebHostEnvironment environment,
    IConfiguration configuration) : IWebAppStartup
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    /// <summary>The hosting environment the application runs in.</summary>
    protected readonly IHostEnvironment Environment = environment;
    /// <summary>The application configuration.</summary>
    protected readonly IConfiguration Configuration = configuration;

    /// <summary>The API display name, used e.g. in the Swagger title.</summary>
    protected abstract string ApiName { get; }

    /// <summary>
    /// Hook to register additional services after the application DI module has been built.
    /// </summary>
    /// <remarks>
    /// The base implementation registers <see cref="Anarchy.Toolkit.Web.Service.Worker.StartupUrlLoggerWorker"/> in the
    /// <c>Development</c> environment. Overrides should call the base implementation.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    protected virtual IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        if (Environment.IsDevelopment())
        {
            services.AddHostedService<StartupUrlLoggerWorker>();
        }

        return services;
    }

    // This method gets called by the runtime. Use this method to add services to the container
    /// <summary>
    /// Registers the application services, controllers, JSON serialization and response compression.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureWebCustomServices(
                DiFactory<TAppSettingsDiModule, TAppSettings>.Build(services, Configuration, Environment))
            .AddCompressionProviders()
            .AddControllers()
            .AddNewtonsoftJson(opt =>
                opt.SerializerSettings.ContractResolver = new MultiCasePropertyNamesContractResolver())
            .AddJsonOptions(opt => {
                opt.JsonSerializerOptions.PropertyNamingPolicy = null;
                opt.JsonSerializerOptions.Converters.Add(new DescribableEnumJsonConverter());
                opt.JsonSerializerOptions.Converters.Add(new NullableDateTimeOffsetJsonConverter());
                opt.JsonSerializerOptions.Converters.Add(new NullableFloatJsonConverter());
                opt.JsonSerializerOptions.Converters.Add(new NullableIntJsonConverter());
                opt.JsonSerializerOptions.Converters.Add(new NullableLongJsonConverter());
                opt.JsonSerializerOptions.Converters.Add(new NullableStringJsonConverter());
            })
            ;
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    /// <inheritdoc/>
    public abstract void Configure(IApplicationBuilder app);
}
