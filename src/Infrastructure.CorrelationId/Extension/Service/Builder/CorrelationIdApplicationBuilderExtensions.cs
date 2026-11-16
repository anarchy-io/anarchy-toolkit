using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Service.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extensions that add <see cref="CorrelationIdMiddleware"/> to the request pipeline.
/// </summary>
/// <remarks>
/// Register the middleware early in the pipeline so that later middleware and logging see the correlation values.
/// </remarks>
/// <example>
/// <code language="csharp">
/// app.UseCorrelationId();
/// app.MapControllers();
/// </code>
/// </example>
public static class CorrelationIdApplicationBuilderExtensions
{
    /// <param name="builder">The application builder.</param>
    extension(IApplicationBuilder builder)
    {
        /// <summary>
        /// Adds <see cref="CorrelationIdMiddleware"/>, resolving <see cref="CorrelationIdOptions"/> from the container.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/>, to allow chaining.</returns>
        /// <remarks>Requires <see cref="CorrelationIdOptions"/> to be registered (e.g. via <c>AddCorrelationId()</c>).</remarks>
        public IApplicationBuilder UseCorrelationId()
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }

        /// <summary>
        /// Adds <see cref="CorrelationIdMiddleware"/> using the given <paramref name="options"/> instead of resolving
        /// them from the container.
        /// </summary>
        /// <param name="options">The options passed to the middleware.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>, to allow chaining.</returns>
        public IApplicationBuilder UseCorrelationId(CorrelationIdOptions options)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>(options);
        }
    }
}
