using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;

/// <summary>
/// Provides abstractions for publishing outbound messages to a message broker.
/// </summary>
/// <remarks>
/// Implementations handle message batching, observability, and fault tolerance based on configuration.
/// Messages may be automatically chunked into smaller batches to comply with broker constraints.
/// </remarks>
public interface IMessageBrokerPublisher
{
    /// <summary>
    /// Callback delegate invoked before each message chunk is published.
    /// </summary>
    /// <param name="messageChunk">
    /// The chunk that will be published next.
    /// This represents a subset of the total messages being published, typically constrained by broker limits.
    /// </param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous callback operation.</returns>
    /// <remarks>
    /// This callback is invoked once per message chunk before publishing is attempted.
    /// Throwing from this callback prevents publish for that chunk and is treated as a chunk failure.
    /// </remarks>
    public delegate Task PublishBatchCallbackBeforeAsync(
        IReadOnlyCollection<OutboundMessageEntity> messageChunk,
        CancellationToken cancellationToken);

    /// <summary>
    /// Callback delegate invoked for each message chunk during batch publishing.
    /// </summary>
    /// <param name="wasSuccessful">
    /// Indicates whether the chunk was successfully published to the message broker.
    /// </param>
    /// <param name="messageChunk">
    /// The messages that were attempted to be published in this chunk.
    /// This represents a subset of the total messages being published, typically constrained by broker limits.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to observe for cancellation requests.
    /// </param>
    /// <returns>A task representing the asynchronous callback operation.</returns>
    /// <remarks>
    /// This callback is invoked once per message chunk, regardless of success or failure.
    /// Chunks are created to respect broker batch size limits during batch publishing operations.
    /// The callback can be used for progress tracking, retry logic, or cleanup operations.
    /// </remarks>
    public delegate Task PublishBatchCallbackAfterAsync(
        bool wasSuccessful,
        IReadOnlyCollection<OutboundMessageEntity> messageChunk,
        CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a single outbound message to the message broker.
    /// </summary>
    /// <param name="outboundMessage">The message to be published.</param>
    /// <param name="cancellationToken">
    /// Optional token to observe for cancellation requests.
    /// If not provided, a new non-cancellable token is used.
    /// </param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    /// <exception cref="MessageBrokerPublisherException">
    /// Thrown when the message fails to publish to the message broker.
    /// </exception>
    /// <remarks>
    /// This method provides a simplified interface for publishing single messages.
    /// Execution duration and success/failure metrics are recorded.
    /// </remarks>
    Task PublishAsync(OutboundMessageEntity outboundMessage,
                      CancellationToken? cancellationToken = null);

    /// <summary>
    /// Publishes multiple outbound messages to the message broker.
    /// </summary>
    /// <param name="outboundMessages">The collection of messages to be published.</param>
    /// <param name="continueOnFailure">
    /// When <c>false</c> (default), throws immediately on the first chunk failure.
    /// When <c>true</c>, continues processing remaining chunks and throws after all chunks are processed if any failures occurred.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token to observe for cancellation requests.
    /// If not provided, a new non-cancellable token is used.
    /// </param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    /// <exception cref="MessageBrokerPublisherException">
    /// Thrown when one or more message chunks fail to publish, depending on the <paramref name="continueOnFailure"/> setting.
    /// With <c>continueOnFailure</c> set to <c>false</c>, this is thrown immediately on first failure.
    /// With <c>continueOnFailure</c> set to <c>true</c>, this is thrown after all chunks are attempted.
    /// </exception>
    /// <remarks>
    /// Messages are automatically chunked to comply with broker batch size limits (e.g., AWS SQS/SNS limits).
    /// Each chunk is processed independently with separate metrics tracking.
    /// Execution duration and success/failure metrics are recorded for each chunk and overall operation.
    /// </remarks>
    Task PublishAsync(IReadOnlyCollection<OutboundMessageEntity> outboundMessages,
                      bool continueOnFailure = false,
                      CancellationToken? cancellationToken = null);

    /// <summary>
    /// Publishes multiple outbound messages to the message broker with optional per-chunk callbacks.
    /// </summary>
    /// <param name="outboundMessages">The collection of messages to be published.</param>
    /// <param name="chunkCallbackBeforeAsync">
    /// Optional callback invoked before each chunk is published.
    /// Use this to perform pre-processing, state transitions, or progress signaling.
    /// </param>
    /// <param name="chunkCallbackAfterAsync">
    /// Optional callback invoked for each message chunk after processing, whether successful or failed.
    /// Use this to track progress, implement retry logic, or perform cleanup operations per chunk.
    /// </param>
    /// <param name="continueOnFailure">
    /// When <c>false</c> (default), throws immediately on the first chunk failure.
    /// When <c>true</c>, continues processing remaining chunks and throws after all chunks are processed if any failures occurred.
    /// Note: The callback is invoked for all chunks regardless of this setting.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token to observe for cancellation requests.
    /// If not provided, a new non-cancellable token is used.
    /// </param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    /// <exception cref="MessageBrokerPublisherException">
    /// Thrown when one or more message chunks fail to publish, depending on the <paramref name="continueOnFailure"/> setting.
    /// With <c>continueOnFailure</c> set to <c>false</c>, this is thrown immediately on first failure.
    /// With <c>continueOnFailure</c> set to <c>true</c>, this is thrown after all chunks are attempted.
    /// </exception>
    /// <remarks>
    /// Messages are automatically chunked to comply with broker batch size limits (e.g., AWS SQS/SNS limits).
    /// You can provide either callback independently or both together.
    /// When both are provided, <paramref name="chunkCallbackBeforeAsync"/> runs before publish and
    /// <paramref name="chunkCallbackAfterAsync"/> runs after the attempt with the success flag.
    /// Each chunk is processed with separate metrics tracking.
    /// Execution duration and success/failure metrics are recorded for each chunk and overall operation.
    /// </remarks>
    Task PublishAsync(IReadOnlyCollection<OutboundMessageEntity> outboundMessages,
                      PublishBatchCallbackBeforeAsync? chunkCallbackBeforeAsync,
                      PublishBatchCallbackAfterAsync? chunkCallbackAfterAsync,
                      bool continueOnFailure = false,
                      CancellationToken? cancellationToken = null);
}
