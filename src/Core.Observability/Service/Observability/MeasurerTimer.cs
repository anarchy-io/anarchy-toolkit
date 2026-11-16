using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// A class that contains information about the started timer.
/// </summary>
/// <remarks>
/// Created by <see cref="MeasurerBase.StartTimer"/> or <see cref="MeasurerBase.StartTimerWithSuccessTag"/>. The elapsed
/// time is recorded through <see cref="MeasurerBase.Timer"/> once, on the first <see cref="Dispose"/>; use it in a
/// <c>using</c> block. Not thread-safe.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var timer = measurer.StartTimer("orders.process", "region:eu");
/// try
/// {
///     await ProcessAsync();
///     timer.TagAsSucceeded();
/// }
/// catch
/// {
///     timer.TagAsFailed();
///     throw;
/// }
/// </code>
/// </example>
public sealed class MeasurerTimer : IDisposable
{
    private const string SuccessTagPrefix = "success:";
    private const string SuccessTagTrue = $"{SuccessTagPrefix}true";
    private const string SuccessTagFalse = $"{SuccessTagPrefix}false";

    private readonly string _name;
    private readonly MeasurerBase _owner;
    private readonly long _timestamp;

    private string[] _tags;
    private string? _successTag;
    private bool _isDisposed;

    internal MeasurerTimer(MeasurerBase owner, string name, string[] tags)
    {
        _owner = owner;
        _name = name;
        _tags = tags;
        _timestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Tags the timer with success tag (<code>success:true</code>), replacing any existing <c>success:</c> tag.
    /// </summary>
    public void TagAsSucceeded() => _successTag = SuccessTagTrue;

    /// <summary>
    /// Tags the timer with failure tag (<code>success:false</code>), replacing any existing <c>success:</c> tag.
    /// </summary>
    public void TagAsFailed() => _successTag = SuccessTagFalse;

    /// <summary>
    /// Stops the timer and records the elapsed time via <see cref="MeasurerBase.Timer"/>. Subsequent calls do nothing.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_successTag is not null)
        {
            var index = Array.FindIndex(_tags, tag => tag.StartsWith(SuccessTagPrefix));

            if (index == -1)
            {
                _tags = [_successTag, .. _tags];
            }
            else
            {
                _tags[index] = _successTag;
            }
        }

        _owner.Timer(_name, Stopwatch.GetElapsedTime(_timestamp), _tags);
        _isDisposed = true;
    }
}
