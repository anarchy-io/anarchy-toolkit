namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Configuration.Common;

/// <summary>
/// Default HTTP client configuration: applies only the common settings of
/// <see cref="HttpClientConfigurationBase{TClient}"/> (base URL, timeout, JSON accept header, timeout handler).
/// </summary>
/// <typeparam name="TClient">Marker type identifying the client/adapter the settings belong to.</typeparam>
public class HttpClientConfiguration<TClient> : HttpClientConfigurationBase<TClient>
{
    private readonly IHttpAdapterSettings<TClient> _settings;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpClientConfiguration{TClient}"/>.
    /// </summary>
    /// <param name="settings">The adapter settings.</param>
    public HttpClientConfiguration(IHttpAdapterSettings<TClient> settings) : base(settings)
    {
        _settings = settings;
    }

    /// <inheritdoc/>
    /// <returns><see cref="IHttpAdapterSettings{TClient}.ClientName"/> from the settings.</returns>
    public override string GetClientName()
    {
        return _settings.ClientName;
    }
}
