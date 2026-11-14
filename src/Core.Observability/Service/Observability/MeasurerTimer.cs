using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// A class that contains information about the started timer.
/// </summary>
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
    /// Tags the timer with success tag (<code>success:true</code>).
    /// </summary>
    public void TagAsSucceeded() => _successTag = SuccessTagTrue;

    /// <summary>
    /// Tags the timer with failure tag (<code>success:false</code>).
    /// </summary>
    public void TagAsFailed() => _successTag = SuccessTagFalse;

    /// <inheritdoc />
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
