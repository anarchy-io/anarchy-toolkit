using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Infrastructure.Http.Service.Http.Handler;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace Anarchy.Toolkit.Infrastructure.Http.Configuration.Common;

/// <summary>
/// Describes how a named <see cref="HttpClient"/> is configured when registered with <see cref="IHttpClientFactory"/>.
/// </summary>
/// <remarks>
/// Implementations pair a client name (used by
/// <see cref="Anarchy.Toolkit.Infrastructure.Http.Repository.Http.HttpRepositoryBase"/> derivatives) with the
/// configuration applied to its <see cref="IHttpClientBuilder"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// IHttpClientConfiguration config = new HttpClientConfiguration&lt;OrdersRepository&gt;(ordersSettings);
/// config.Configure(services.AddHttpClient(config.GetClientName()));
/// </code>
/// </example>
public interface IHttpClientConfiguration
{
    /// <summary>
    /// Gets the name under which the client is registered with <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <returns>The client name.</returns>
    string GetClientName();

    /// <summary>
    /// Applies this configuration to the given client builder.
    /// </summary>
    /// <param name="builder">The builder returned by <c>services.AddHttpClient(name)</c>.</param>
    /// <returns>The builder, to allow further chaining.</returns>
    IHttpClientBuilder Configure(IHttpClientBuilder builder);
}

/// <summary>
/// Base <see cref="IHttpClientConfiguration"/> that applies the common settings from
/// <see cref="IHttpAdapterSettings{TClient}"/>.
/// </summary>
/// <remarks>
/// <see cref="Configure"/> validates the settings, then sets <see cref="HttpClient.Timeout"/> from
/// <see cref="IHttpAdapterSettings{TClient}.DefaultTimeoutMSec"/>, <see cref="HttpClient.BaseAddress"/> from
/// <see cref="IHttpAdapterSettings{TClient}.BaseUrl"/>, adds an <c>Accept: application/json</c> header and appends an
/// <see cref="HttpTimeoutHandler"/>. Derived classes override <see cref="Configure"/> to add more handlers (e.g.
/// authentication), calling the base implementation first.
/// </remarks>
/// <typeparam name="TClient">Marker type identifying the client/adapter the settings belong to.</typeparam>
/// <param name="settings">The adapter settings.</param>
public abstract class HttpClientConfigurationBase<TClient>(IHttpAdapterSettings<TClient> settings)
    : IHttpClientConfiguration
{
    /// <inheritdoc/>
    public abstract string GetClientName();

    /// <inheritdoc/>
    /// <exception cref="Anarchy.Toolkit.Core.Exceptions.EmptyArgumentException">
    /// <see cref="IHttpAdapterSettings{TClient}.BaseUrl"/> is null, empty or whitespace.
    /// </exception>
    /// <exception cref="Anarchy.Toolkit.Core.Exceptions.ArgumentCheckException">
    /// <see cref="IHttpAdapterSettings{TClient}.DefaultTimeoutMSec"/> is not greater than zero.
    /// </exception>
    public virtual IHttpClientBuilder Configure(IHttpClientBuilder builder)
    {
        ValidateSettings();

        return builder
            .ConfigureHttpClient((_, client) => {
                client.Timeout = TimeSpan.FromMilliseconds(settings.DefaultTimeoutMSec);
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler(_ => new HttpTimeoutHandler());
    }

    private void ValidateSettings()
    {
        var msg = $"HttpClient {GetClientName()} configuration is invalid";

        settings.BaseUrl.CheckNotEmpty(nameof(settings.BaseUrl), msg);
        (settings.DefaultTimeoutMSec > 0).CheckTrue(nameof(settings.DefaultTimeoutMSec), msg);
    }
}
