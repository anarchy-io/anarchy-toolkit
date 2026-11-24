using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helper.MessageBroker;
using Confluent.Kafka;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Service.MessageBroker;

/// <summary>Publishes messages to Kafka. The supplied producer is owned by its caller.</summary>
public sealed class KafkaMessageBrokerPublisher(IProducer<string, string> producer, AppTelemetry telemetry) : IMessageBrokerPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync(OutboundMessage outboundMessage, CancellationToken? cancellationToken = null)
    {
        if (outboundMessage is null) throw new KafkaNullArgumentException(nameof(outboundMessage));
        var caller = this.CalledFrom();
        var timer = Stopwatch.StartNew();
        var success = false;
        try
        {
            var token = cancellationToken ?? CancellationToken.None;
            token.ThrowIfCancellationRequested();
            await producer.ProduceAsync(outboundMessage.Destination, KafkaMessageMapper.ToKafka(outboundMessage), token);
            success = true;
        }
        catch (OperationCanceledException) when (cancellationToken?.IsCancellationRequested == true)
        {
            throw;
        }
        catch (Exception ex)
        {
            telemetry.LogError(caller, ex, "Failed to publish Kafka message to {Destination}", outboundMessage.Destination);
            throw new MessagePublishException(ex);
        }
        finally
        {
            KeyValuePair<string, object?>[] tags = [new("success", success)];
            telemetry.RecordDuration(caller, timer.Elapsed, tags);
            telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, tags);
        }
    }

    /// <inheritdoc/>
    public Task PublishAsync(IReadOnlyCollection<OutboundMessage> outboundMessages, bool continueOnFailure = false,
        CancellationToken? cancellationToken = null) =>
        PublishAsync(outboundMessages, null, null, continueOnFailure, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Each record is a separate chunk; callbacks run once per attempted record. No atomic batch is implied.</remarks>
    public async Task PublishAsync(IReadOnlyCollection<OutboundMessage> outboundMessages,
        IMessageBrokerPublisher.PublishBatchCallbackBeforeAsync? chunkCallbackBeforeAsync,
        IMessageBrokerPublisher.PublishBatchCallbackAfterAsync? chunkCallbackAfterAsync,
        bool continueOnFailure = false, CancellationToken? cancellationToken = null)
    {
        if (outboundMessages is null) throw new KafkaNullArgumentException(nameof(outboundMessages));
        var caller = this.CalledFrom();
        var timer = Stopwatch.StartNew();
        var success = false;
        try
        {
            await PublishBatchAsync(outboundMessages, chunkCallbackBeforeAsync, chunkCallbackAfterAsync,
                continueOnFailure, cancellationToken ?? CancellationToken.None);
            success = true;
        }
        finally
        {
            KeyValuePair<string, object?>[] tags = [new("success", success)];
            telemetry.RecordDuration(caller, timer.Elapsed, tags);
            telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, tags);
        }
    }

    private async Task PublishBatchAsync(IReadOnlyCollection<OutboundMessage> outboundMessages,
        IMessageBrokerPublisher.PublishBatchCallbackBeforeAsync? chunkCallbackBeforeAsync,
        IMessageBrokerPublisher.PublishBatchCallbackAfterAsync? chunkCallbackAfterAsync,
        bool continueOnFailure, CancellationToken token)
    {
        var failed = 0;
        Exception? lastFailure = null;
        foreach (var message in outboundMessages)
        {
            token.ThrowIfCancellationRequested();
            OutboundMessage[] chunk = [message];
            Exception? failure = null;
            try
            {
                if (chunkCallbackBeforeAsync is not null)
                    await chunkCallbackBeforeAsync(chunk, token);
                await PublishAsync(message, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            try
            {
                if (chunkCallbackAfterAsync is not null)
                    await chunkCallbackAfterAsync(failure is null, chunk, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failure = failure is null ? ex : new AggregateException(failure, ex);
            }

            if (failure is null) continue;
            failed++;
            lastFailure = failure;
            if (!continueOnFailure) throw new MessageBatchPublishException(failed, failure);
        }

        if (lastFailure is not null) throw new MessageBatchPublishException(failed, lastFailure);
    }
}
