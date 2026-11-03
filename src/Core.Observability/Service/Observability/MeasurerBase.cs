using Anarchy.Toolkit.Core.Exceptions;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// A base class for raw application metrics.
/// </summary>
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
    public static IDisposable BeginScope(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new Scope(tag);
    }

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
    public abstract void IncrementCounter(string name, params string[] tags);

    /// <summary>
    /// Increments the specified <c>Counter</c> metric by the amount indicated by
    /// <paramref name="amount"/>.
    /// </summary>
    public abstract void IncrementCounter(string name, int amount, params string[] tags);

    /// <summary>
    /// Sets the current value for a <c>Gauge</c>-type metric, which represents something like a sensor readout.
    /// (For example: a temperature sensor, the current CPU utilization or the current number of items in a queue.)
    /// This data may be aggregated and submitted only as the latest sample recorded within a certain time window
    /// per unique set of <paramref name="tags"/>.
    /// </summary>
    public abstract void Gauge(string name, double value, params string[] tags);

    /// <summary>
    /// Registers a value for a <c>Histogram</c>-type metric, representing some statistical data.
    /// (For example: the size of a message payload, the total value of an order or the number of products in an order.)
    /// This data may be aggregated and submitted as a collection of <c>Gauge</c>-type metrics representing certain
    /// statistical aspects of the metric like <c>max</c>, <c>median</c>, <c>avg</c>, <c>95percentile</c> or <c>count</c>
    /// per unique set of <paramref name="tags"/> within a certain time window.
    /// </summary>
    public abstract void Histogram(string name, double value, params string[] tags);

    /// <summary>
    /// Registers an elapsed time for a timer-type metric.
    /// This data may be submitted as a <c>Histogram</c>-typed metric.
    /// </summary>
    public abstract void Timer(string name, TimeSpan elapsedTime, params string[] tags);

    /// <summary>
    /// Registers an event.
    /// </summary>
    public abstract void Event(string title, string text, params string[] tags);

    /// <summary>
    /// Starts a timer that will register an elapsed time for a timer-type metric when the
    /// result is disposed.
    /// </summary>
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
