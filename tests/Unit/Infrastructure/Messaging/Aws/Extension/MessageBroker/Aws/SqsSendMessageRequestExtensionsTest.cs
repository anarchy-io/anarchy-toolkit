using Amazon.SQS.Model;
using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;

public class SqsSendMessageRequestExtensionsTest : TestCaseBase
{
    private const string NotGoodBeforeTimestamp = "NotGoodBeforeTimestamp";

    [Fact]
    public void SetAttributes_WhenNoNotGoodBeforeTimestamp_DoesNotSetDelaySeconds()
    {
        // Arrange
        var request = new SendMessageRequest();
        var attributes = new Dictionary<string, MessageAttributeValue> {
            ["Foo"] = new() { DataType = "String", StringValue = "Bar" }
        };

        // Act
        request.SetAttributes(attributes);

        // Assert
        request.MessageAttributes.Should().BeSameAs(attributes);
        request.DelaySeconds.Should().BeNull();
    }

    [Fact]
    public void SetAttributes_WhenTimestampIsInvalid_DoesNotSetDelaySeconds()
    {
        // Arrange
        var request = new SendMessageRequest();
        var attributes = new Dictionary<string, MessageAttributeValue> {
            [NotGoodBeforeTimestamp] = new() {
                DataType = "String",
                StringValue = "this-is-not-a-timestamp"
            }
        };

        // Act
        request.SetAttributes(attributes);

        // Assert
        request.MessageAttributes.Should().BeSameAs(attributes);
        request.DelaySeconds.Should().BeNull();
    }

    [Fact]
    public void SetAttributes_WhenTimestampIsInPast_DoesNotSetDelaySeconds()
    {
        // Arrange
        var request = new SendMessageRequest();
        var past = DateHelper.Now().AddSeconds(-30);
        var attributes = new Dictionary<string, MessageAttributeValue> {
            [NotGoodBeforeTimestamp] = new() {
                DataType = "String",
                StringValue = past.ToString("O")
            }
        };

        // Act
        request.SetAttributes(attributes);

        // Assert
        request.MessageAttributes.Should().BeSameAs(attributes);
        request.DelaySeconds.Should().BeNull();
    }

    [Fact]
    public void SetAttributes_WhenTimestampIsInNearFuture_SetsPositiveDelaySeconds()
    {
        // Arrange
        var request = new SendMessageRequest();
        var target = DateHelper.Now().AddSeconds(30);
        var attributes = new Dictionary<string, MessageAttributeValue> {
            [NotGoodBeforeTimestamp] = new() {
                DataType = "String",
                StringValue = target.ToString("O")
            }
        };

        // Act
        request.SetAttributes(attributes);

        // Assert
        request.MessageAttributes.Should().BeSameAs(attributes);
        // Allow a bit of drift between constructing the timestamp and calling DateHelper.Now()
        request.DelaySeconds.Should().BeGreaterThan(0);
        request.DelaySeconds.Should().BeLessThanOrEqualTo(30);
    }

    [Fact]
    public void SetAttributes_WhenTimestampIsFarInFuture_CapsDelaySecondsAtSqsMax()
    {
        // Arrange
        var request = new SendMessageRequest();
        var target = DateHelper.Now().AddSeconds(2000); // > 900
        var attributes = new Dictionary<string, MessageAttributeValue> {
            [NotGoodBeforeTimestamp] = new() {
                DataType = "String",
                StringValue = target.ToString("O")
            }
        };

        // Act
        request.SetAttributes(attributes);

        // Assert
        request.MessageAttributes.Should().BeSameAs(attributes);
        request.DelaySeconds.Should().Be(900);
    }
}
