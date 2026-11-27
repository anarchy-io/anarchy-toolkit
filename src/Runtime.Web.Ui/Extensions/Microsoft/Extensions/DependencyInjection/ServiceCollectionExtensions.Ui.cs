using Anarchy.Toolkit.Runtime.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Core.Service.Ui.Session;
using Anarchy.Toolkit.Runtime.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Model.Ui;
using Anarchy.Toolkit.Runtime.Web.Ui.Service.Host.Ui.Session;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Anarchy.Toolkit.Runtime.Web.Ui.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring UI authentication.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the cookie-based UI session: OAuth client-credentials login against the selected environment's auth
        /// service, the environment catalog and <see cref="IUiSessionContext"/>.
        /// </summary>
        /// <remarks>
        /// Always registers <paramref name="options"/> as a singleton, <see cref="IUiEnvironmentCatalog"/> and
        /// <see cref="IUiSessionContext"/>. When <see cref="UiAuthenticationOptions.EnableCookieAuth"/> is set, it also
        /// registers the <see cref="UiAuthHandler.HttpClientName"/> HTTP client (with
        /// <see cref="UiAuthenticationOptions.AuthServiceTimeout"/>), <see cref="UiAuthHandler"/>,
        /// <see cref="UiSessionHandler"/>, cookie authentication configured from <paramref name="options"/>, and
        /// authorization.
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// services.AddToolkitUiAuthentication(new UiAuthenticationOptions {
        ///     Scope = "my-api.read",
        ///     Environments = [
        ///         new UiEnvironment("Testing", new Uri("https://auth.testing.example.com/"),
        ///             new Dictionary&lt;string, Uri&gt; { ["orders"] = new Uri("https://orders.testing.example.com/") })
        ///     ]
        /// });
        /// </code>
        /// </example>
        /// <param name="options">The UI authentication options; defaults to a new <see cref="UiAuthenticationOptions"/>.</param>
        /// <returns>The service collection, for chaining.</returns>
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
