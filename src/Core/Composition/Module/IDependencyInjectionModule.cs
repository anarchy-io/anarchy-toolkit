using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Core.Composition.Module;

public interface IDependencyInjectionModule
{
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                        IHostEnvironment environment);
}
