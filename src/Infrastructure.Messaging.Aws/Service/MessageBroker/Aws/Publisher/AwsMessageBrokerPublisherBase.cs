using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher;

public interface IAwsMessageBrokerPublisher<TRequest> : IMessageBrokerPublisher;

public abstract class AwsMessageBrokerPublisherBase<TRequest, TBatchRequest>(
    IAwsPublisherRequestBuilder<TRequest, TBatchRequest> builderPublisherRequest,
    Observer observer) : IAwsMessageBrokerPublisher<TRequest>
{
    private const int MaxBatchSize = 10; // AWS limit for batch operations

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

    protected abstract Task SendRequestAsync(TRequest request, CancellationToken cancellationToken);
    protected abstract Task SendBatchRequestAsync(TBatchRequest batchRequest, CancellationToken cancellationToken);
}
