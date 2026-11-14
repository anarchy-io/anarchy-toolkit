namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;

public static class CorrelationContext
{
    public static string? CorrelationId => Values.Value?.CorrelationId;
    public static string? AttemptId => Values.Value?.AttemptId;

    public static void SetCorrelationValues(string? correlationId, string? attemptId)
    {
        var values = new CorrelationValues(correlationId: correlationId, attemptId: attemptId);

        Values.Value = values;
    }

    private static readonly AsyncLocal<CorrelationValues?> Values = new();

    private class CorrelationValues
    {
        public string? CorrelationId { get; }
        public string? AttemptId { get; }

        public CorrelationValues(string? correlationId, string? attemptId)
        {
            CorrelationId = correlationId;
            AttemptId = attemptId;
        }
    }
}
