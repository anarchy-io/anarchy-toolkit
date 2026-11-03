using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Hosting;

public interface IStartup
{
    void ConfigureServices(IServiceCollection services);
}
