using Anarchy.Toolkit.Core.Health.Service.Health;
using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
    /// /health/services, and /health/status).
    /// </summary>
    public class ApplicationHealthMiddlewareWithCustomMetadata : ApplicationHealthMiddlewareBase
    {
        /// <summary>
        /// Create a new instance of <see cref="ApplicationHealthMiddlewareWithCustomMetadata"/>.
        /// </summary>
        public ApplicationHealthMiddlewareWithCustomMetadata(
            RequestDelegate next,
            IReadOnlyCollection<IServiceHealthReport> reports,
            IBuildMetadata buildMetadata,
            DateTimeOffset applicationStartTime) : base(next)
        {
            _reports = reports;
            _buildMetadata = buildMetadata;
            _applicationStartTime = applicationStartTime;
        }

        /// <summary>
        /// Handle a HTTP request with custom metadata
        /// </summary>
        public Task Invoke(HttpContext context)
        {
            var healthApi = ApplicationHealthApi.WithCustomMetadata(_reports, _buildMetadata, _applicationStartTime);
            return base.InvokeWithHealthApi(context, healthApi);
        }

        private readonly IReadOnlyCollection<IServiceHealthReport> _reports;
        private readonly IBuildMetadata _buildMetadata;
        private readonly DateTimeOffset _applicationStartTime;
    }
}
