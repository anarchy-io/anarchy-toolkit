using Amazon.SimpleNotificationService;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Microsoft.Extensions.DependencyInjection;
using SnsBatchRequest = Amazon.SimpleNotificationService.Model.PublishBatchRequest;
using SnsRequest = Amazon.SimpleNotificationService.Model.PublishRequest;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.Microsoft.DependencyInjection;

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
