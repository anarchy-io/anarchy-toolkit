using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.DependencyInjection.Module;

/// <summary>
/// Registers the AWS Sns message broker publisher (keyed <c>topic</c> <c>IMessageBrokerPublisher</c>).
/// The <see cref="AwsMessagingOptions"/> are resolved from the application's configuration through the given factory.
/// </summary>
public sealed class ModuleSnsMessageBroker(Func<IConfiguration, AwsMessagingOptions> options) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services.AddSnsMessageBroker(options(configuration));
    }
}
