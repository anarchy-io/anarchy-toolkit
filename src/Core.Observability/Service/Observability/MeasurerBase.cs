using Anarchy.Toolkit.Core.Exceptions;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// A base class for raw application metrics.
/// </summary>
/// <remarks>
/// <para>Implementations forward metrics to a concrete backend (see <see cref="LoggerMeasurer"/> for a log-based
/// implementation). Tags use the <c>name:value</c> format; <see cref="Tags"/> helps build them.</para>
/// <para>Ambient tags can be added with <see cref="BeginScope"/>; they are stored in an <see cref="AsyncLocal{T}"/> and
/// flow with the current async context. Implementations should call <see cref="GetTags"/> to merge them into each
/// metric.</para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using (MeasurerBase.BeginScope("tenant:acme"))
/// {
///     measurer.IncrementCounter("orders.received", "channel:web");
///     measurer.Histogram("orders.total_value", order.Total);
/// }
/// </code>
/// </example>
public abstract class MeasurerBase
{
    private static readonly string[] SuccessTag = ["success:true"];

    private static readonly AsyncLocal<ImmutableList<KeyValuePair<Guid, string>>> ScopeStorage = new() { Value = [] };

    private static ImmutableList<KeyValuePair<Guid, string>> ScopeStorageValue {
        get => ScopeStorage.Value ??= [];
        set => ScopeStorage.Value = value;
    }

    /// <summary>
    /// Gets the ambient tags pushed by every <see cref="BeginScope"/> still open in the current async flow.
    /// </summary>
    public static IEnumerable<string> ScopedTags => ScopeStorageValue.Select(e => e.Value);

    /// <summary>
    /// Opens a scope whose <paramref name="tag"/> is included in all metrics recorded until the result is disposed.
    /// </summary>
    /// <param name="tag">The ambient tag, in <c>name:value</c> format.</param>
    /// <returns>A handle that removes the tag when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tag"/> is <see langword="null"/>.</exception>
    public static IDisposable BeginScope(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new Scope(tag);
    }

    /// <summary>
    /// Appends the current <see cref="ScopedTags"/> to <paramref name="tags"/>.
    /// </summary>
    /// <param name="tags">The tags supplied for a metric.</param>
    /// <returns><paramref name="tags"/> followed by the ambient scoped tags (or <paramref name="tags"/> itself when there are none).</returns>
    protected static string[] GetTags(string[] tags)
    {
        var currentTags = ScopedTags.ToArray();

        return currentTags.Length > 0
            ? [.. tags, .. currentTags]
            : tags;
    }

    /// <summary>
    /// Increments the specified <c>Counter</c> metric by 1.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public abstract void IncrementCounter(string name, params string[] tags);

    /// <summary>
    /// Increments the specified <c>Counter</c> metric by the amount indicated by
    /// <paramref name="amount"/>.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="amount">The amount to increment by.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public abstract void IncrementCounter(string name, int amount, params string[] tags);

    /// <summary>
    /// Sets the current value for a <c>Gauge</c>-type metric, which represents something like a sensor readout.
    /// (For example: a temperature sensor, the current CPU utilization or the current number of items in a queue.)
    /// This data may be aggregated and submitted only as the latest sample recorded within a certain time window
    /// per unique set of <paramref name="tags"/>.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The current value.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public abstract void Gauge(string name, double value, params string[] tags);

    /// <summary>
    /// Registers a value for a <c>Histogram</c>-type metric, representing some statistical data.
    /// (For example: the size of a message payload, the total value of an order or the number of products in an order.)
    /// This data may be aggregated and submitted as a collection of <c>Gauge</c>-type metrics representing certain
    /// statistical aspects of the metric like <c>max</c>, <c>median</c>, <c>avg</c>, <c>95percentile</c> or <c>count</c>
    /// per unique set of <paramref name="tags"/> within a certain time window.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The sampled value.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public abstract void Histogram(string name, double value, params string[] tags);

    /// <summary>
    /// Registers an elapsed time for a timer-type metric.
    /// This data may be submitted as a <c>Histogram</c>-typed metric.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="elapsedTime">The measured duration.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public abstract void Timer(string name, TimeSpan elapsedTime, params string[] tags);

    /// <summary>
    /// Registers an event.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="text">The event description.</param>
    /// <param name="tags">The tags, in <c>name:value</c> format.</param>
    public abstract void Event(string title, string text, params string[] tags);

    /// <summary>
    /// Starts a timer that will register an elapsed time for a timer-type metric when the
    /// result is disposed.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="tags">The tags to apply to the timer.</param>
    /// <returns>A running timer; dispose it to record the elapsed time.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if
    /// <list type="bullet">
    /// <item><paramref name="name"/> is an empty string.</item>
    /// <item><paramref name="tags"/> contains an element that is <c>null</c>.</item>
    /// </list>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> or <paramref name="tags"/> is <c>null</c>.
    /// </exception>
    public MeasurerTimer StartTimer(string name, params string[] tags)
    {
        ThrowIfArgumentsInvalid(name, tags);

        return new MeasurerTimer(this, name: name, tags: tags);
    }

    /// <summary>
    /// Starts a timer that will register an elapsed time for a timer-type metric when the
    /// result is disposed. The timer will be tagged with <code>success:true</code> tag
    /// as well as the specified <paramref name="tags"/>
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="tags">The tags to apply to the timer.</param>
    /// <returns>A running timer; dispose it to record the elapsed time.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if
    /// <list type="bullet">
    /// <item><paramref name="name"/> is an empty string.</item>
    /// <item><paramref name="tags"/> contains an element that is <c>null</c>.</item>
    /// </list>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> or <paramref name="tags"/> is <c>null</c>.
    /// </exception>
    public MeasurerTimer StartTimerWithSuccessTag(string name, params string[] tags)
    {
        ThrowIfArgumentsInvalid(name, tags);

        return new MeasurerTimer(this, name: name, tags: [.. SuccessTag, .. tags]);
    }

    /// <exception cref="ArgumentException">
    /// Thrown if
    /// <list type="bullet">
    /// <item><paramref name="name"/> is an empty string.</item>
    /// <item><paramref name="tags"/> contains an element that is <c>null</c>.</item>
    /// </list>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> or <paramref name="tags"/> is <c>null</c>.
    /// </exception>
    private static void ThrowIfArgumentsInvalid(string name, string[] tags)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name == "")
        {
            throw new EmptyArgumentException(nameof(name), "Name cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(tags);

        if (tags.Any(t => t is null))
        {
            throw new EmptyArgumentException(nameof(tags), "Tag cannot be null.");
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly Guid _id = Guid.NewGuid();

        public Scope(string tag) =>
            ScopeStorageValue = ScopeStorageValue.Add(new KeyValuePair<Guid, string>(_id, tag));

        public void Dispose() =>
            ScopeStorageValue = ScopeStorageValue.RemoveAll(e => e.Key == _id);
    }
}
