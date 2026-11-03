using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Web.Entity.Security;
using Anarchy.Toolkit.Web.Service.Host.Authentication;
using Anarchy.Toolkit.Web.Service.SwaggerGen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Anarchy.Toolkit.Web.Extension.DependencyInjection;

public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers JWT bearer authentication. On TAP the token is validated against the
        /// configured auth authority; locally an unsigned JWT handler is used for development.
        /// </summary>
        public IServiceCollection AddToolkitAuthentication(IAuthServiceAppSettings appSettings, IHostEnvironment environment)
        {
            return environment.IsLocal() ? services.AddAuthLocal(appSettings) : services.AddAuthTap(appSettings);
        }

        /// <summary>
        /// Configures the generic Admin/Read/Write authorization policies.
        /// Scope strings are supplied by the consumer (no domain scopes are hardcoded).
        /// </summary>
        /// <param name="readScope">Scope granting read access.</param>
        /// <param name="writeScope">Scope granting write access.</param>
        /// <param name="adminScope">Scope granting admin access (also satisfies read and write).</param>
        public IServiceCollection SetupAuthorization(string readScope,
                                                     string writeScope,
                                                     string adminScope)
        {
            return services
                    .AddAuthorization(options => {
                        options.AddPolicy(ApiAccess.Policy.Admin, p => p.RequireScope(adminScope));
                        options.AddPolicy(ApiAccess.Policy.Read, p => p.RequireScope(adminScope, readScope));
                        options.AddPolicy(ApiAccess.Policy.Write, p => p.RequireScope(adminScope, writeScope));
                    })
                ;
        }

        /// <summary>
        /// Registers Swagger generation for the REST API.
        /// </summary>
        /// <param name="apiName">The API display name.</param>
        /// <param name="appSettings">Auth service settings (provide the OAuth authority).</param>
        /// <param name="environment">The host environment.</param>
        /// <param name="oauthScopes">
        /// Consumer-supplied OAuth2 scopes offered by the Swagger UI client-credentials flow.
        /// </param>
        /// <param name="titlePrefix">
        /// Bracketed prefix for the Swagger document title (e.g. "[MyApi]"). Defaults to "[API]".
        /// </param>
        /// <param name="descriptionOptions">Optional links rendered in the Swagger description.</param>
        public IServiceCollection RegisterSwagger(string apiName,
                                                  IAuthServiceAppSettings appSettings,
                                                  IHostEnvironment environment,
                                                  IReadOnlyCollection<string>? oauthScopes = null,
                                                  string titlePrefix = "[API]",
                                                  SwaggerDescriptionOptions? descriptionOptions = null)
        {
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
                c.AddSecurityDefinition("oauth2",
                    new OpenApiSecurityScheme {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows {
                            ClientCredentials = new OpenApiOAuthFlow {
                                TokenUrl = new Uri(appSettings.AuthService.Host, "connect/token"),
                                Scopes = (oauthScopes ?? [])
                                    .ToDictionary(scope => scope, _ => string.Empty)
                            }
                        }
                    });

                // Authorization Requirements (policies)
                c.OperationFilter<AuthorizeCheckOperationFilter>();

                // Advertise Accept-Encoding support so generated clients request compressed responses by default
                c.OperationFilter<AcceptEncodingHeaderOperationFilter>();

                c.AddSwaggerBaseUrls();
            });
        }

        private IServiceCollection AddAuthTap(IAuthServiceAppSettings appSettings)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
                    options.Authority = appSettings.AuthService.Host.ToString();
                    options.RequireHttpsMetadata = appSettings.AuthService.Host.Scheme == Uri.UriSchemeHttps;
                    options.Events = JwtBearerEventsBuilder.Build();
                    options.TokenValidationParameters = new TokenValidationParameters {
                        ValidateAudience = false
                    };
                });

            return services;
        }

        private IServiceCollection AddAuthLocal(IAuthServiceAppSettings appSettings)
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<JwtBearerOptions, UnsignedJwtAuthenticationHandler>(
                    JwtBearerDefaults.AuthenticationScheme,
                    options => {
                        options.Authority = appSettings.AuthService.Host.ToString();
                        options.RequireHttpsMetadata = false; // Not required for local dev
                        options.Events = JwtBearerEventsBuilder.Build();
                    });

            return services;
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
