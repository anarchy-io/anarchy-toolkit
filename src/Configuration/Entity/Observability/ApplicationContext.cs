using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;

namespace Anarchy.Toolkit.Configuration.Entity.Observability;

public class ApplicationContext : IApplicationContext
{
    public static readonly string FallbackCorrelationId = Guid.NewGuid()
        .ToString();

    public static readonly string FallbackAttemptId = Guid.NewGuid()
        .ToString();

    public string RequestCorrelationId {
        get => CorrelationContext.CorrelationId ?? FallbackCorrelationId;
    }

    public string RequestAttemptId {
        get => CorrelationContext.AttemptId ?? FallbackAttemptId;
    }

    public string AmazonTraceId {
        get => AmazonRequestContext.AmazonTraceId;
    }
}
