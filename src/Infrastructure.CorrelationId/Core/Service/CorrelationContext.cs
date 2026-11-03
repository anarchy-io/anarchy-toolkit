namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service
{
    public static class CorrelationContext
    {
        public static string? CorrelationId => _values.Value?.CorrelationId;
        public static string? AttemptId => _values.Value?.AttemptId;

        public static void SetCorrelationValues(string? correlationId, string? attemptId)
        {
            var values = new CorrelationValues(correlationId: correlationId, attemptId: attemptId);

            _values.Value = values;
        }

        private static readonly AsyncLocal<CorrelationValues?> _values = new AsyncLocal<CorrelationValues?>();

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
}
