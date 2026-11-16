using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

/// <summary>
/// AWS-specific <see cref="IMessageBrokerPublisher"/>, parameterised by the SDK request type so SNS and SQS
/// publishers can be resolved independently from the container.
/// </summary>
/// <typeparam name="TRequest">The AWS single-message request type that identifies the transport.</typeparam>
public interface IAwsMessageBrokerPublisher<TRequest> : IMessageBrokerPublisher;

/// <summary>
/// Base class for AWS message-broker publishers. Builds SDK requests via an
/// <see cref="IAwsPublisherRequestBuilder{TRequest, TBatchRequest}"/>, sends them through the transport-specific
/// <see cref="SendRequestAsync"/>/<see cref="SendBatchRequestAsync"/>, and records logs and metrics through the
/// <see cref="Observer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Batch publishing splits the messages into chunks of 10 (the AWS batch limit) and sends them sequentially. The
/// optional "before" callback runs before each chunk is sent, and the "after" callback runs after each chunk with a
/// success flag. Duration and count metrics are recorded per chunk (suffix <c>.chunk</c>) and for the whole call.
/// </para>
/// <para>
/// When <c>continueOnFailure</c> is <see langword="false"/>, the first failing chunk stops publishing; otherwise the
/// remaining chunks are still sent and a single exception is raised at the end. A warning is logged when
/// <c>continueOnFailure</c> is used with FIFO destinations, since ordering may be broken.
/// </para>
/// <para>
/// Note: only the request-level call is checked; partial failures reported inside a successful AWS batch response
/// are not inspected.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The AWS single-message request type.</typeparam>
/// <typeparam name="TBatchRequest">The AWS batch request type.</typeparam>
/// <param name="builderPublisherRequest">Builds AWS requests from outbound messages.</param>
/// <param name="observer">The observer used for logging and metrics.</param>
public abstract class AwsMessageBrokerPublisherBase<TRequest, TBatchRequest>(
    IAwsPublisherRequestBuilder<TRequest, TBatchRequest> builderPublisherRequest,
    Observer observer) : IAwsMessageBrokerPublisher<TRequest>
{
    private const int MaxBatchSize = 10; // AWS limit for batch operations

    /// <inheritdoc/>
    /// <exception cref="MessagePublishException">Building or sending the request failed; wraps the original exception.</exception>
    public async Task PublishAsync(OutboundMessageEntity outboundMessage,
                                   CancellationToken? cancellationToken = null)
    {
        var caller = this.CalledFrom();
        var requestType = typeof(TRequest).Name.ToSnakeCase();
        var timer = Stopwatch.StartNew();

        try
        {
            await SendRequestAsync(builderPublisherRequest.FromOutboundMessage(outboundMessage),
                cancellationToken ?? CancellationToken.None);

            observer.LogInformation(
                (CallSiteEntity)caller,
                "Published outbound message for request_type {RequestType} with message_count {MessageCount}",
                requestType,
                1);

            observer.ExecutionRecordDuration(
                (CallSiteEntity)caller,
                timer.Elapsed,
                Tags.Success);

            observer.ExecutionIncrementCount((CallSiteEntity)caller, 1, Tags.Success);
        }
        catch (Exception ex)
        {
            observer.ExecutionRecordDuration(
                (CallSiteEntity)caller,
                timer.Elapsed,
                Tags.Failure);

            observer.ExecutionIncrementCount((CallSiteEntity)caller, 1, Tags.Failure);
            observer.LogError(
                ex,
                (CallSiteEntity)caller,
                "Failed to publish outbound message for request_type {RequestType} with message_count {MessageCount}",
                requestType,
                1);

            throw new MessagePublishException(ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="MessageBatchPublishException">One or more chunks failed to publish.</exception>
    public async Task PublishAsync(
        IReadOnlyCollection<OutboundMessageEntity> outboundMessages,
        bool continueOnFailure = false,
        CancellationToken? cancellationToken = null)
    {
        await PublishBatchInternalAsync(
            outboundMessages,
            continueOnFailure,
            null,
            null,
            cancellationToken);
    }

    /// <inheritdoc/>
    /// <exception cref="MessageBatchPublishException">One or more chunks failed to publish.</exception>
    public async Task PublishAsync(
        IReadOnlyCollection<OutboundMessageEntity> outboundMessages,
        IMessageBrokerPublisher.PublishBatchCallbackBeforeAsync? chunkCallbackBeforeAsync,
        IMessageBrokerPublisher.PublishBatchCallbackAfterAsync? chunkCallbackAfterAsync,
        bool continueOnFailure = false,
        CancellationToken? cancellationToken = null)
    {
        await PublishBatchInternalAsync(outboundMessages, continueOnFailure, chunkCallbackBeforeAsync,
            chunkCallbackAfterAsync, cancellationToken);
    }

    private async Task PublishBatchInternalAsync(
        IReadOnlyCollection<OutboundMessageEntity> outboundMessages,
        bool continueOnFailure,
        IMessageBrokerPublisher.PublishBatchCallbackBeforeAsync? chunkCallbackBeforeAsync = null,
        IMessageBrokerPublisher.PublishBatchCallbackAfterAsync? chunkCallbackAfterAsync = null,
        CancellationToken? cancellationToken = null)
    {
        var caller = this.CalledFrom();
        var requestType = typeof(TRequest).Name;
        var timer = Stopwatch.StartNew();
        Exception? lastException = null;
        var totalPublished = 0;
        var totalFailed = 0;

        if (continueOnFailure && outboundMessages.Any(m => m.Destination.IsFifo()))
        {
            observer.LogWarning(caller,
                "continueOnFailure is enabled but messages contain FIFO destinations. This may lead to out-of-order processing.");
        }

        try
        {
            // Split messages into chunks of max batch size
            var batches = outboundMessages
                .Chunk(MaxBatchSize)
                .ToList();

            foreach (var batch in batches)
            {
                var chunkTimer = Stopwatch.StartNew();

                try
                {
                    var batchRequest = builderPublisherRequest.FromOutboundMessage(batch);
                    if (chunkCallbackBeforeAsync != null)
                    {
                        await chunkCallbackBeforeAsync(batch, cancellationToken ?? CancellationToken.None);
                    }

                    await SendBatchRequestAsync(batchRequest, cancellationToken ?? CancellationToken.None);

                    await RecordChunkSuccessAsync(caller, chunkTimer, requestType, batch, chunkCallbackAfterAsync,
                        cancellationToken);

                    totalPublished += batch.Length;
                }
                catch (Exception ex)
                {
                    await RecordChunkFailureAsync(caller, chunkTimer, requestType, batch, ex, chunkCallbackAfterAsync,
                        cancellationToken);

                    totalFailed += batch.Length;
                    lastException = ex;

                    if (!continueOnFailure)
                    {
                        throw new MessageBatchPublishException(totalFailed, ex);
                    }
                }
            }

            if (lastException != null)
            {
                throw new MessageBatchPublishException(totalFailed, lastException);
            }
        }
        finally
        {
            RecordFinalMetrics(caller, timer, lastException, totalPublished, totalFailed);
        }
    }

    private async Task RecordChunkSuccessAsync(
        CallSiteEntity caller,
        Stopwatch chunkTimer,
        string requestType,
        OutboundMessageEntity[] batch,
        IMessageBrokerPublisher.PublishBatchCallbackAfterAsync? chunkCallbackAsync,
        CancellationToken? cancellationToken)
    {
        var chunkCaller = $"{caller.ToMetricPrefix()}.chunk";

        observer.LogInformation(caller,
            "Published outbound message batch for request_type {RequestType} with message_count {MessageCount}",
            requestType,
            batch.Length);

        observer.ExecutionRecordDuration(chunkCaller, chunkTimer.Elapsed, Tags.Success);
        observer.ExecutionIncrementCount(chunkCaller, batch.Length, Tags.Success);

        if (chunkCallbackAsync != null)
        {
            await chunkCallbackAsync(true, batch, cancellationToken ?? CancellationToken.None);
        }
    }

    private async Task RecordChunkFailureAsync(
        CallSiteEntity caller,
        Stopwatch chunkTimer,
        string requestType,
        OutboundMessageEntity[] batch,
        Exception ex,
        IMessageBrokerPublisher.PublishBatchCallbackAfterAsync? chunkCallbackAsync,
        CancellationToken? cancellationToken)
    {
        var chunkCaller = $"{caller.ToMetricPrefix()}.chunk";

        observer.ExecutionRecordDuration(chunkCaller, chunkTimer.Elapsed, Tags.Failure);
        observer.ExecutionIncrementCount(chunkCaller, batch.Length, Tags.Failure);

        observer.LogError(
            ex,
            caller,
            "Failed to publish outbound message batch for request_type {RequestType} with message_count {MessageCount}",
            requestType,
            batch.Length);

        if (chunkCallbackAsync != null)
        {
            await chunkCallbackAsync(false, batch, cancellationToken ?? CancellationToken.None);
        }
    }

    private void RecordFinalMetrics(
        CallSiteEntity caller,
        Stopwatch timer,
        Exception? lastException,
        int totalPublished,
        int totalFailed)
    {
        observer.ExecutionRecordDuration(caller, timer.Elapsed, lastException == null ? Tags.Success : Tags.Failure);

        if (totalPublished > 0)
        {
            observer.ExecutionIncrementCount(caller, totalPublished, Tags.Success);
        }

        if (totalFailed > 0)
        {
            observer.ExecutionIncrementCount(caller, totalFailed, Tags.Failure);
        }
    }

    /// <summary>
    /// Sends a single-message request through the AWS SDK client.
    /// </summary>
    /// <param name="request">The request built from the outbound message.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>A task that completes when AWS has accepted the request.</returns>
    protected abstract Task SendRequestAsync(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a batch request (up to 10 entries) through the AWS SDK client.
    /// </summary>
    /// <param name="batchRequest">The batch request built from a chunk of outbound messages.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>A task that completes when AWS has responded to the batch request.</returns>
    protected abstract Task SendBatchRequestAsync(TBatchRequest batchRequest, CancellationToken cancellationToken);
}
