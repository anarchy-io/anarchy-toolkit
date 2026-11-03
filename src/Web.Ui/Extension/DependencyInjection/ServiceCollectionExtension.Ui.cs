using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui.Session;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Model.Ui;
using Anarchy.Toolkit.Web.Ui.Service.Host.Ui.Session;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Anarchy.Toolkit.Web.Ui.Extension.DependencyInjection;

/// <summary>
/// Extension methods for configuring UI authentication.
/// </summary>
public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the cookie-based UI session: OAuth client-credentials login against the selected environment's auth
        /// service, the environment catalog and <see cref="IUiSessionContext"/>.
        /// </summary>
        public IServiceCollection AddToolkitUiAuthentication(UiAuthenticationOptions? options = null)
        {
            options ??= new UiAuthenticationOptions();

            services
                .AddSingleton(options)
                .AddSingleton<IUiEnvironmentCatalog, UiEnvironmentCatalog>()
                .AddScoped<IUiSessionContext, UiSessionContext>();

            if (!options.EnableCookieAuth)
            {
                return services;
            }

            services.AddHttpClient(UiAuthHandler.HttpClientName, client => client.Timeout = options.AuthServiceTimeout);

            services
                .AddScoped<UiAuthHandler>()
                .AddScoped<UiSessionHandler>()
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(cookieOptions => {
                    cookieOptions.LoginPath = options.LoginPath;
                    cookieOptions.LogoutPath = options.LogoutPath;
                    cookieOptions.Cookie.HttpOnly = options.HttpOnly;
                    cookieOptions.Cookie.IsEssential = options.IsEssential;
                    cookieOptions.Cookie.SecurePolicy = options.SecurePolicy;
                    cookieOptions.Cookie.SameSite = options.SameSite;
                });

            services.AddAuthorization();

            return services;
        }
    }
}
