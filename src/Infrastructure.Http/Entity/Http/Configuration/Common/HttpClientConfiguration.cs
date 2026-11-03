namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Configuration.Common;

public class HttpClientConfiguration<TClient> : HttpClientConfigurationBase<TClient>
{
    private readonly IHttpAdapterSettings<TClient> _settings;

    public HttpClientConfiguration(IHttpAdapterSettings<TClient> settings) : base(settings)
    {
        _settings = settings;
    }

    public override string GetClientName()
    {
        return _settings.ClientName;
    }
}
