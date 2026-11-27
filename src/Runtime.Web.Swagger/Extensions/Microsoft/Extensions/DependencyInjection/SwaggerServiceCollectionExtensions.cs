using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Runtime.Web.Swagger.Composition;
using Anarchy.Toolkit.Runtime.Web.Swagger.Service.SwaggerGen;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Anarchy.Toolkit.Runtime.Web.Swagger.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that register the toolkit OpenAPI document and Swagger UI.
/// </summary>
public static class SwaggerServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Enables Swagger on a toolkit REST API: registers the OpenAPI document (<see cref="AddToolkitSwaggerGen"/>)
        /// and puts the Swagger UI (<c>UseToolkitSwagger</c>) in front of the application's pipeline.
        /// </summary>
        /// <param name="apiName">The API display name.</param>
        /// <param name="appSettings">The application settings; an OAuth2 scheme is declared when they implement <see cref="IAuthServiceAppSettings"/>.</param>
        /// <param name="environment">The host environment.</param>
        /// <param name="oauthScopes">Consumer-supplied OAuth2 scopes offered by the Swagger UI client-credentials flow.</param>
        /// <param name="titlePrefix">Bracketed prefix for the document and UI titles (e.g. "[MyApi]"). Defaults to "[API]".</param>
        /// <param name="descriptionOptions">Optional links rendered in the Swagger description.</param>
        /// <returns>The service collection, for chaining.</returns>
        /// <example>
        /// <code language="csharp">
        /// protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services) =&gt;
        ///     base.ConfigureWebCustomServices(services)
        ///         .AddToolkitSwagger(ApiName, AppSettings, Environment);
        /// </code>
        /// </example>
        public IServiceCollection AddToolkitSwagger(string apiName,
                                                    IAppSettings appSettings,
                                                    IHostEnvironment environment,
                                                    IReadOnlyCollection<string>? oauthScopes = null,
                                                    string titlePrefix = "[API]",
                                                    SwaggerDescriptionOptions? descriptionOptions = null)
        {
            return services
                .AddToolkitSwaggerGen(apiName, appSettings, environment, oauthScopes, titlePrefix, descriptionOptions)
                .AddTransient<IStartupFilter>(_ => new SwaggerStartupFilter(apiName, titlePrefix));
        }

        /// <summary>
        /// Registers Swagger generation for the REST API, without changing the pipeline.
        /// </summary>
        /// <remarks>
        /// Creates a single <c>spec</c> document, includes every <c>*.xml</c> documentation file found in
        /// <see cref="AppContext.BaseDirectory"/>, declares an <c>oauth2</c> client-credentials scheme whose token URL
        /// is <c>{AuthService.Host}/connect/token</c>, applies <see cref="AuthorizeCheckOperationFilter"/> and
        /// <see cref="AcceptEncodingHeaderOperationFilter"/>, and adds a server entry from the <c>BASE_PATH</c>
        /// environment variable when it is set. The document is served as OpenAPI 3.2 by default; override
        /// <see cref="SwaggerOptions.OpenApiVersion"/> via <c>services.Configure&lt;SwaggerOptions&gt;</c>. Serve it
        /// with <c>UseToolkitSwagger</c>, or use <see cref="AddToolkitSwagger"/> to do both.
        /// </remarks>
        /// <param name="apiName">The API display name.</param>
        /// <param name="appSettings">The application settings; an OAuth2 scheme is declared when they implement <see cref="IAuthServiceAppSettings"/>.</param>
        /// <param name="environment">The host environment.</param>
        /// <param name="oauthScopes">Consumer-supplied OAuth2 scopes offered by the Swagger UI client-credentials flow.</param>
        /// <param name="titlePrefix">Bracketed prefix for the document title (e.g. "[MyApi]"). Defaults to "[API]".</param>
        /// <param name="descriptionOptions">Optional links rendered in the Swagger description.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddToolkitSwaggerGen(string apiName,
                                                       IAppSettings appSettings,
                                                       IHostEnvironment environment,
                                                       IReadOnlyCollection<string>? oauthScopes = null,
                                                       string titlePrefix = "[API]",
                                                       SwaggerDescriptionOptions? descriptionOptions = null)
        {
            // Serve OpenAPI 3.2 by default; consumers can override via services.Configure<SwaggerOptions>(...)
            services.Configure<SwaggerOptions>(o => o.OpenApiVersion = OpenApiSpecVersion.OpenApi3_2);

            return services.AddSwaggerGen(c => {
                // Root Document
                c.SwaggerDoc("spec", new OpenApiInfo {
                    Title = $"{titlePrefix} {apiName}",
                    Version = "1.0.1",
                    License = new OpenApiLicense {
                        Name = "UNLICENSED"
                    },
                    Description = SwaggerDescriptionBuilder.Build(appSettings, environment, descriptionOptions)
                });

                // DocBlocks
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    c.IncludeXmlComments(xmlFile);
                }

                // Authentication
                if (appSettings is IAuthServiceAppSettings authSettings)
                {
                    c.AddSecurityDefinition("oauth2",
                        new OpenApiSecurityScheme {
                            Type = SecuritySchemeType.OAuth2,
                            Flows = new OpenApiOAuthFlows {
                                ClientCredentials = new OpenApiOAuthFlow {
                                    TokenUrl = new Uri(authSettings.AuthService.Host, "connect/token"),
                                    Scopes = (oauthScopes ?? [])
                                        .ToDictionary(scope => scope, _ => string.Empty)
                                }
                            }
                        });
                }

                // Authorization Requirements (policies)
                c.OperationFilter<AuthorizeCheckOperationFilter>();

                // Advertise Accept-Encoding support so generated clients request compressed responses by default
                c.OperationFilter<AcceptEncodingHeaderOperationFilter>();

                c.AddSwaggerBaseUrls();
            });
        }
    }

    /// <summary>
    ///     Adds a server entry to the Swagger documentation based on the BASE_PATH environment variable.
    /// </summary>
    /// <remarks>
    ///     If the BASE_PATH environment variable is not set, no server will be added.
    ///     This allows the Swagger UI to default to the current origin when running locally.
    /// </remarks>
    private static void AddSwaggerBaseUrls(this SwaggerGenOptions options)
    {
        var basePath = Environment.GetEnvironmentVariable("BASE_PATH");
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return;
        }

        options.AddServer(new OpenApiServer {
            Url = basePath
        });
    }
}
