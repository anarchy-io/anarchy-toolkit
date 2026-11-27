using Anarchy.Toolkit.Runtime.Web.MockServer.Entity;
using System.Text.Json;

namespace Anarchy.Toolkit.Runtime.Web.MockServer.Service;

/// <summary>
/// Keeps the received requests in memory (up to <see cref="MockServerOptions.RecordLimit"/>) and, when
/// <see cref="MockServerOptions.RecordFile"/> is set, appends each one to that file as a JSON line. Thread-safe.
/// </summary>
public sealed class RequestRecorder
{
    private readonly Lock _lock = new();
    private readonly LinkedList<RecordedRequest> _requests = [];
    private readonly int _limit;
    private readonly string? _file;
    private long _sequence;

    /// <summary>Creates a recorder; truncates the record file when there is one.</summary>
    /// <param name="options">The server options.</param>
    public RequestRecorder(MockServerOptions options)
    {
        _limit = options.RecordLimit;
        if (options.RecordFile is { Length: > 0 } file)
        {
            _file = Path.GetFullPath(file);
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            System.IO.File.WriteAllText(_file, "");
        }
    }

    /// <summary>The record file's full path, or <see langword="null"/>.</summary>
    public string? File => _file;

    /// <summary>The recorded requests, oldest first.</summary>
    public IReadOnlyList<RecordedRequest> All {
        get {
            lock (_lock) return _requests.ToList();
        }
    }

    /// <summary>Records a request, assigning its <see cref="RecordedRequest.Sequence"/>.</summary>
    /// <param name="request">The request.</param>
    public void Record(RecordedRequest request)
    {
        lock (_lock)
        {
            request.Sequence = ++_sequence;
            _requests.AddLast(request);
            if (_limit > 0 && _requests.Count > _limit) _requests.RemoveFirst();
            if (_file is not null) System.IO.File.AppendAllText(_file, JsonSerializer.Serialize(request, MockServerJson.Compact) + "\n");
        }
    }

    /// <summary>Forgets the in-memory requests (the record file is kept).</summary>
    /// <returns>How many were forgotten.</returns>
    public int Clear()
    {
        lock (_lock)
        {
            var count = _requests.Count;
            _requests.Clear();
            return count;
        }
    }
}
