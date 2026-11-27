using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that register the correlation-id services.
/// </summary>
/// <remarks>
/// Registers <see cref="CorrelationIdOptions"/> as a singleton so that
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.CorrelationIdMiddleware"/> can resolve
/// it. Pair with <c>UseCorrelationId()</c> on the application builder.
/// </remarks>
/// <example>
/// <code language="csharp">
/// builder.Services.AddCorrelationId();
/// var app = builder.Build();
/// app.UseCorrelationId();
/// </code>
/// </example>
public static class CorrelationIdServiceCollectionExtensions
{
    /// <param name="services">The service collection to register into.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a default <see cref="CorrelationIdOptions"/> instance as a singleton.
        /// </summary>
        public void AddCorrelationId()
        {
            var options = new CorrelationIdOptions();
            services.AddCorrelationId(options);
        }

        /// <summary>
        /// Registers the given <see cref="CorrelationIdOptions"/> instance as a singleton.
        /// </summary>
        /// <param name="options">The options to register.</param>
        public void AddCorrelationId(CorrelationIdOptions options)
        {
            services.AddSingleton(options);
        }

        /// <summary>
        /// Invokes a custom registration delegate against the service collection, letting the caller register
        /// <see cref="CorrelationIdOptions"/> (or related services) however it needs.
        /// </summary>
        /// <param name="registrationDelegate">Delegate that performs the registrations.</param>
        /// <remarks>Nothing is registered automatically; the delegate is responsible for registering the options.</remarks>
        public void AddCorrelationId(Action<IServiceCollection> registrationDelegate)
        {
            registrationDelegate(services);
        }
    }
}
