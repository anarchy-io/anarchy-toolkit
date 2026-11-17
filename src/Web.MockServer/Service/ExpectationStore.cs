using Anarchy.Toolkit.Web.MockServer.Entity;

namespace Anarchy.Toolkit.Web.MockServer.Service;

/// <summary>
/// The expectations the server currently answers with: those loaded from files or code, plus those added at runtime
/// through <c>PUT /__mock/expectations</c>. Thread-safe.
/// </summary>
public sealed class ExpectationStore
{
    private readonly Lock _lock = new();
    private List<MockExpectation> _loaded = [];
    private readonly List<MockExpectation> _runtime = [];

    /// <summary>Every expectation, runtime ones first.</summary>
    public IReadOnlyList<MockExpectation> All {
        get {
            lock (_lock) return [.. _runtime, .. _loaded];
        }
    }

    /// <summary>Replaces the expectations loaded from files or code.</summary>
    /// <param name="expectations">The new expectations.</param>
    public void ReplaceLoaded(IEnumerable<MockExpectation> expectations)
    {
        lock (_lock) _loaded = expectations.ToList();
    }

    /// <summary>Adds expectations at runtime; they take precedence over loaded ones of the same priority.</summary>
    /// <param name="expectations">The expectations.</param>
    public void AddRuntime(IEnumerable<MockExpectation> expectations)
    {
        lock (_lock)
        {
            foreach (var expectation in expectations)
            {
                if (expectation.Source == "code") expectation.Source = "runtime";
                _runtime.Insert(0, expectation);
            }
        }
    }

    /// <summary>Removes the runtime expectations, or only the one with <paramref name="id"/>.</summary>
    /// <param name="id">The expectation id, or <see langword="null"/> for all.</param>
    /// <returns>How many were removed.</returns>
    public int ClearRuntime(string? id = null)
    {
        lock (_lock) return id is null ? ClearAll() : _runtime.RemoveAll(e => e.Id == id);

        int ClearAll()
        {
            var count = _runtime.Count;
            _runtime.Clear();
            return count;
        }
    }

    /// <summary>
    /// Finds the expectation answering <paramref name="request"/>: highest priority first, then the most specific path,
    /// then runtime before loaded and earlier before later. Consumes one of its remaining times.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The expectation, or <see langword="null"/> when none matches.</returns>
    public MockExpectation? FindMatch(MockRequest request)
    {
        lock (_lock)
        {
            var match = _runtime.Concat(_loaded)
                .Where(e => e.Times is not { Unlimited: false, RemainingTimes: <= 0 })
                .Select((e, index) => (Expectation: e, Index: index, Path: ExpectationMatcher.Match(e.HttpRequest, request)))
                .Where(c => c.Path != ExpectationMatcher.PathMatch.None)
                .OrderByDescending(c => c.Expectation.Priority)
                .ThenByDescending(c => c.Path)
                .ThenBy(c => c.Index)
                .Select(c => c.Expectation)
                .FirstOrDefault();

            if (match?.Times is { Unlimited: false } times) times.RemainingTimes--;
            return match;
        }
    }
}
