namespace Anarchy.Toolkit.Configuration.Core.Service;

public interface IApplicationContext
{
    string RequestCorrelationId { get; }
    string RequestAttemptId { get; }
    string AmazonTraceId { get; }
}
