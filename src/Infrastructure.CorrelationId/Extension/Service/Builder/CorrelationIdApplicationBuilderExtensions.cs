using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Service.Builder;

public static class CorrelationIdApplicationBuilderExtensions
{
    extension(IApplicationBuilder builder)
    {
        public IApplicationBuilder UseCorrelationId()
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }

        public IApplicationBuilder UseCorrelationId(CorrelationIdOptions options)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>(options);
        }
    }
}
