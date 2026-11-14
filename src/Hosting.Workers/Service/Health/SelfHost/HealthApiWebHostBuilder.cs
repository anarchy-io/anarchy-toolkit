using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost;

internal class HealthApiWebHostBuilder
{
    public HealthApiWebHostBuilder(int portNumber, Action<IApplicationBuilder> configureAction)
    {
        _portNumber = portNumber;
        _configureAction = configureAction;
    }

    public IHost CreateWebHost()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(b =>
                b.UseUrls(CreateHostUrl())
                    .Configure(ConfigureApplication))
            .UseSystemd();

        return builder.Build();
    }

    private string CreateHostUrl()
    {
        return $"http://*:{_portNumber}";
    }

    private void ConfigureApplication(IApplicationBuilder app)
    {
        _configureAction(app);

        var requestHandler = new DefaultRequestHandler();
        app.Run(DefaultRequestHandler.HandleRequestAsync);
    }

    private readonly int _portNumber;
    private readonly Action<IApplicationBuilder> _configureAction;
}
