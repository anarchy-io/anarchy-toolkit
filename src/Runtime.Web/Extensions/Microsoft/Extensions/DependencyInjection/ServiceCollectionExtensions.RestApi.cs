using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Runtime.Web.Entity.Security;
using Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Authorization;
using Anarchy.Toolkit.Runtime.Web.Service.Host.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that register the toolkit web services (authentication,
/// authorization, response compression and UI services).
/// </summary>
public static partial class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers JWT bearer authentication. Remotely the token is validated against the
        /// configured auth authority; locally an unsigned JWT handler is used for development.
        /// </summary>
        /// <remarks>
        /// Both modes register the <see cref="JwtBearerDefaults.AuthenticationScheme"/> scheme with
        /// <see cref="Anarchy.Toolkit.Runtime.Web.Service.Host.Authentication.JwtBearerEventsBuilder.Build"/> events. Remotely the
        /// authority is <c>AuthService.Host</c>, HTTPS metadata is required only for <c>https</c> authorities and the
        /// audience is not validated. In the local environment tokens are read <b>without signature validation</b>.
        /// </remarks>
        /// <param name="appSettings">Auth service settings providing the OAuth authority (<c>AuthService.Host</c>).</param>
        /// <param name="environment">The host environment; selects the local or remote handler.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddToolkitAuthentication(IAuthServiceAppSettings appSettings, IHostEnvironment environment)
        {
            return environment.IsLocal() ? services.AddAuthLocal(appSettings) : services.AddAuthRemote(appSettings);
        }

        /// <summary>
        /// Configures the generic Admin/Read/Write authorization policies.
        /// Scope strings are supplied by the consumer (no domain scopes are hardcoded).
        /// </summary>
        /// <param name="readScope">Scope granting read access.</param>
        /// <param name="writeScope">Scope granting write access.</param>
        /// <param name="adminScope">Scope granting admin access (also satisfies read and write).</param>
        /// <returns>The service collection, for chaining.</returns>
        /// <example>
        /// <code language="csharp">
        /// services.SetupAuthorization("my-api.read", "my-api.write", "my-api.admin");
        ///
        /// [Authorize(Policy = ApiAccess.Policy.Read)]
        /// public class OrdersController : ControllerBase { }
        /// </code>
        /// </example>
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

        private IServiceCollection AddAuthRemote(IAuthServiceAppSettings appSettings)
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
}
