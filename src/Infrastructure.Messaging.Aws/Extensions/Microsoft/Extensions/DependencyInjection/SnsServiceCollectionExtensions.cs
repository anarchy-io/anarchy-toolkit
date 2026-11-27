using Amazon.SimpleNotificationService;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Microsoft.Extensions.DependencyInjection;
using SnsBatchRequest = Amazon.SimpleNotificationService.Model.PublishBatchRequest;
using SnsRequest = Amazon.SimpleNotificationService.Model.PublishRequest;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration helpers for the AWS SNS message-broker publisher.
/// </summary>
public static class SnsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SNS message-broker publisher, its request builder and the
    /// underlying <see cref="IAmazonSimpleNotificationService"/> client.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="options">
    /// The AWS account/region context used to build topic ARNs.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <paramref name="options"/> as a singleton, a scoped <see cref="AmazonSimpleNotificationServiceClient"/>
    /// using the default AWS credential and region resolution, a scoped
    /// <see cref="SnsAwsPublisherRequestBuilder"/> (gzip compression for oversized messages), a scoped
    /// <see cref="SnsMessageBrokerPublisher"/> and a keyed (<c>"topic"</c>) <see cref="IMessageBrokerPublisher"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// services.AddSnsMessageBroker(new AwsMessagingOptions("eu-west-1", "123456789012"));
    ///
    /// public class OrderNotifier([FromKeyedServices("topic")] IMessageBrokerPublisher publisher) { }
    /// </code>
    /// </example>
    public static IServiceCollection AddSnsMessageBroker(this IServiceCollection services, AwsMessagingOptions options)
    {
        services.AddSingleton(options);

        AddClients(services);
        AddRequestBuilders(services);
        AddMessagePublishers(services);

        return services;
    }

    private static void AddClients(IServiceCollection services)
    {
        services.AddScoped<IAmazonSimpleNotificationService>(_ => new AmazonSimpleNotificationServiceClient());
    }

    private static void AddRequestBuilders(IServiceCollection services)
    {
        services.AddScoped<IAwsPublisherRequestBuilder<SnsRequest, SnsBatchRequest>, SnsAwsPublisherRequestBuilder>();
    }

    private static void AddMessagePublishers(IServiceCollection services)
    {
        services
            .AddScoped<IAwsMessageBrokerPublisher<SnsRequest>, SnsMessageBrokerPublisher>()
            .AddKeyedScoped<IMessageBrokerPublisher>("topic",
                (sp, _) => sp.GetRequiredService<IAwsMessageBrokerPublisher<SnsRequest>>());
    }
}
