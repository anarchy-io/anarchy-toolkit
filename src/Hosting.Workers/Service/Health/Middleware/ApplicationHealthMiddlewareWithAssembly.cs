using Anarchy.Toolkit.Core.Health.Service.Health;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
    /// /health/services, and /health/status).
    /// </summary>
    public class ApplicationHealthMiddlewareWithMetadataFromAssembly : ApplicationHealthMiddlewareBase
    {
        /// <summary>
        /// Create a new instance of <see cref="ApplicationHealthMiddlewareWithMetadataFromAssembly"/>.
        /// </summary>
        public ApplicationHealthMiddlewareWithMetadataFromAssembly(
            RequestDelegate next,
            IReadOnlyCollection<IServiceHealthReport> reports,
            Assembly assembly,
            DateTimeOffset applicationStartTime) : base(next)
        {
            _reports = reports;
            _assembly = assembly;
            _applicationStartTime = applicationStartTime;
        }

        /// <summary>
        /// Handle a HTTP request with custom metadata
        /// </summary>
        public Task Invoke(HttpContext context)
        {
            var healthApi = ApplicationHealthApi.WithMetadataFromAssembly(_reports, _assembly, _applicationStartTime);
            return base.InvokeWithHealthApi(context, healthApi);
        }

        private readonly IReadOnlyCollection<IServiceHealthReport> _reports;
        private readonly Assembly _assembly;
        private readonly DateTimeOffset _applicationStartTime;
    }
}
