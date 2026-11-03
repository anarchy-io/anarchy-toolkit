using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Microsoft.DependencyInjection;

public static class CorrelationIdServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddCorrelationId()
        {
            var options = new CorrelationIdOptions();
            services.AddCorrelationId(options);
        }

        public void AddCorrelationId(CorrelationIdOptions options)
        {
            services.AddSingleton(options);
        }

        public void AddCorrelationId(Action<IServiceCollection> registrationDelegate)
        {
            registrationDelegate(services);
        }
    }
}
