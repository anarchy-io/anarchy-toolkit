using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extensions.Core.Messaging.Abstractions.Entity.MessageBroker;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Extensions.Core.Messaging.Abstractions.Entity.MessageBroker;

public class InboundMessageCorrelationExtensionsTest
{
    [Fact]
    public async Task BeginCorrelationScope_UsesMessageAttributesAcrossAwait_ThenRestoresContext()
    {
        CorrelationContext.SetCorrelationValues(null, null);
        var message = new InboundMessage(
            "id",
            "receipt",
            "body",
            "queue",
            attributes: new Dictionary<string, string> {
                ["CorrelationId"] = "message-correlation",
                ["AttemptId"] = "message-attempt"
            });

        using (message.BeginCorrelationScope(NullLogger.Instance))
        {
            await Task.Yield();
            CorrelationContext.CorrelationId.Should().Be("message-correlation");
            CorrelationContext.AttemptId.Should().Be("message-attempt");
        }

        CorrelationContext.CorrelationId.Should().BeNull();
        CorrelationContext.AttemptId.Should().BeNull();
    }

    [Fact]
    public void BeginCorrelationScope_UsesCustomAttributeAndLogPropertyNames()
    {
        var message = new InboundMessage(
            "id",
            "receipt",
            "body",
            "queue",
            attributes: new Dictionary<string, string> {
                ["RequestId"] = "message-correlation",
                ["RequestAttempt"] = "message-attempt"
            });

        using (message.BeginCorrelationScope(NullLogger.Instance, "RequestId", "RequestAttempt"))
        {
            CorrelationContext.CorrelationId.Should().Be("message-correlation");
            CorrelationContext.AttemptId.Should().Be("message-attempt");
        }
    }
}
