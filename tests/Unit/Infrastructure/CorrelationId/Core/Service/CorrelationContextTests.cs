using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.CorrelationId.Core.Service;

public class CorrelationContextTests
{
    [Fact]
    public async Task BeginScope_SetsValuesAndActivityTagsAcrossAwait_ThenRestoresContextAndPreservesTraceTags()
    {
        CorrelationContext.SetCorrelationValues("outer-correlation", "outer-attempt");
        using var activity = new Activity("correlation-test");
        activity.SetTag("RequestId", "previous-correlation-tag");
        activity.Start();

        try
        {
            using (CorrelationContext.BeginScope(NullLogger.Instance, "inner-correlation", "inner-attempt",
                       "RequestId", "RequestAttempt"))
            {
                await Task.Yield();

                CorrelationContext.CorrelationId.Should().Be("inner-correlation");
                CorrelationContext.AttemptId.Should().Be("inner-attempt");
                activity.GetTagItem("RequestId").Should().Be("inner-correlation");
                activity.GetTagItem("RequestAttempt").Should().Be("inner-attempt");
            }

            CorrelationContext.CorrelationId.Should().Be("outer-correlation");
            CorrelationContext.AttemptId.Should().Be("outer-attempt");
            activity.GetTagItem("RequestId").Should().Be("previous-correlation-tag");
            activity.GetTagItem("RequestAttempt").Should().Be("inner-attempt");
        }
        finally
        {
            CorrelationContext.SetCorrelationValues(null, null);
        }
    }
}
