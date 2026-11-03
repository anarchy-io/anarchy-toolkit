using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Hosting.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;
using Anarchy.Toolkit.Web.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Presentation.Converter.Json;
using Anarchy.Toolkit.Web.Service.Worker;

namespace Anarchy.Toolkit.Web.Composition;

public abstract class WebAppStartupBase<TAppSettingsDiModule, TAppSettings>(
    IWebHostEnvironment environment,
    IConfiguration configuration) : IWebAppStartup
    where TAppSettingsDiModule : class, IAppSettingsDiModule<TAppSettings>, new()
    where TAppSettings : class, IAppSettings
{
    protected readonly IHostEnvironment Environment = environment;
    protected readonly IConfiguration Configuration = configuration;

    protected abstract string ApiName { get; }

    protected virtual IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        if (Environment.IsDevelopment())
        {
            services.AddHostedService<StartupUrlLoggerWorker>();
        }

        return services;
    }

    // This method gets called by the runtime. Use this method to add services to the container
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
    public abstract void Configure(IApplicationBuilder app);
}
