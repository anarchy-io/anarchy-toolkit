using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;

public class CorrelationIdOptions
{
    public CorrelationIdBehavior Behavior { get; set; }
    public string CorrelationIdHeaderName { get; set; }
    public string AttemptIdHeaderName { get; set; }

    public CorrelationIdOptions()
    {
        Behavior = CorrelationIdBehavior.GenerateValues;
        CorrelationIdHeaderName = CorrelationHeaderNames.CorrelationId;
        AttemptIdHeaderName = CorrelationHeaderNames.AttemptId;
    }
}

public enum CorrelationIdBehavior
{
    GenerateValues = 0,
    IgnoreIfMissing = 2,
    ThrowIfMissing = 3
}
