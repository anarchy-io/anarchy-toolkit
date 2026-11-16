using Amazon.SQS;
using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.Microsoft.DependencyInjection;

/// <summary>
/// Registration helpers for the AWS SQS message-broker publisher.
/// </summary>
public static class SqsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQS <see cref="IAmazonSQS"/> client, the SQS request builder and the
    /// <see cref="SqsMessageBrokerPublisher"/> against the message-broker publisher abstraction.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="options">The AWS account/region context used to build queue URLs.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <remarks>
    /// Registers <paramref name="options"/> as a singleton, a scoped <see cref="AmazonSQSClient"/> using the default
    /// AWS credential and region resolution, a scoped <see cref="SqsAwsPublisherRequestBuilder"/> (gzip compression
    /// for oversized messages), a scoped <see cref="SqsMessageBrokerPublisher"/> and a keyed (<c>"queue"</c>)
    /// <see cref="IMessageBrokerPublisher"/>.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// services.AddSqsMessageBroker(new AwsMessagingOptions("eu-west-1", "123456789012"));
    ///
    /// public class OrderSender([FromKeyedServices("queue")] IMessageBrokerPublisher publisher) { }
    /// </code>
    /// </example>
    public static IServiceCollection AddSqsMessageBroker(this IServiceCollection services, AwsMessagingOptions options)
    {
        services.AddSingleton(options);

        services.AddScoped<IAmazonSQS>(_ => new AmazonSQSClient());

        services.AddScoped<IAwsPublisherRequestBuilder<SendMessageRequest, SendMessageBatchRequest>>(
            sp => new SqsAwsPublisherRequestBuilder(sp.GetRequiredService<AwsMessagingOptions>()));

        services.AddScoped<IAwsMessageBrokerPublisher<SendMessageRequest>, SqsMessageBrokerPublisher>();

        services.AddKeyedScoped<IMessageBrokerPublisher>("queue",
            (sp, _) => sp.GetRequiredService<IAwsMessageBrokerPublisher<SendMessageRequest>>());

        return services;
    }
}
