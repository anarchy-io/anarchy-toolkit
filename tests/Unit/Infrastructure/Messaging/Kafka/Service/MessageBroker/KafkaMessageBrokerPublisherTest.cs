using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Service.MessageBroker;
using AwesomeAssertions;
using Confluent.Kafka;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Kafka.Service.MessageBroker;

public class KafkaMessageBrokerPublisherTest
{
    [Fact]
    public async Task PublishAsync_WhenArgumentsAreNull_ThrowsSpecificNullArgumentException()
    {
        IMessageBrokerPublisher publisher = new KafkaMessageBrokerPublisher(new Mock<IProducer<string, string>>().Object, AppTelemetry.Null);
        var single = () => publisher.PublishAsync((OutboundMessage)null!, TestContext.Current.CancellationToken);
        var batch = () => publisher.PublishAsync((IReadOnlyCollection<OutboundMessage>)null!, cancellationToken: TestContext.Current.CancellationToken);

        var singleException = (await single.Should().ThrowExactlyAsync<KafkaNullArgumentException>()).Which;
        singleException.ParamName.Should().Be("outboundMessage");
        singleException.Should().BeAssignableTo<ArgumentNullException>();
        var batchException = (await batch.Should().ThrowExactlyAsync<KafkaNullArgumentException>()).Which;
        batchException.ParamName.Should().Be("outboundMessages");
        batchException.Should().BeAssignableTo<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WhenMessagesHaveDifferentDestinations_SendsEachAndCallsCallbacks()
    {
        var producer = new Mock<IProducer<string, string>>();
        var sent = new List<string>();
        producer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .Returns((string topic, Message<string, string> _, CancellationToken _) => {
                sent.Add(topic);
                return Task.FromResult(new DeliveryResult<string, string>());
            });
        IMessageBrokerPublisher publisher = new KafkaMessageBrokerPublisher(producer.Object, AppTelemetry.Null);
        var outcomes = new List<string>();
        OutboundMessage[] messages = [new("one", "first", "body"), new("two", "second", "body")];

        await publisher.PublishAsync(messages,
            (chunk, _) => { outcomes.Add("before:" + chunk.Single().Id); return Task.CompletedTask; },
            (ok, chunk, _) => { outcomes.Add($"after:{chunk.Single().Id}:{ok}"); return Task.CompletedTask; },
            cancellationToken: TestContext.Current.CancellationToken);

        sent.Should().Equal("first", "second");
        outcomes.Should().Equal("before:one", "after:one:True", "before:two", "after:two:True");
    }

    [Fact]
    public async Task PublishAsync_WhenBeforeFailsAndContinueEnabled_ReportsFailureAndContinues()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        IMessageBrokerPublisher publisher = new KafkaMessageBrokerPublisher(producer.Object, AppTelemetry.Null);
        var outcomes = new List<bool>();
        OutboundMessage[] messages = [new("one", "orders", "body"), new("two", "orders", "body")];

        var act = () => publisher.PublishAsync(messages,
            (chunk, _) => chunk.Single().Id == "one" ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask,
            (ok, _, _) => { outcomes.Add(ok); return Task.CompletedTask; }, true, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<MessageBatchPublishException>();
        exception.Which.FailedMessageCount.Should().Be(1);
        outcomes.Should().Equal(false, true);
        producer.Verify(p => p.ProduceAsync("orders", It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCancelled_DoesNotSend()
    {
        var producer = new Mock<IProducer<string, string>>();
        IMessageBrokerPublisher publisher = new KafkaMessageBrokerPublisher(producer.Object, AppTelemetry.Null);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var act = () => publisher.PublishAsync(new OutboundMessage("id", "orders", "body"), source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        producer.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WhenProducerFails_StopsAndReportsFailedChunk()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker unavailable"));
        IMessageBrokerPublisher publisher = new KafkaMessageBrokerPublisher(producer.Object, AppTelemetry.Null);
        var outcomes = new List<bool>();
        OutboundMessage[] messages = [new("one", "orders", "body"), new("two", "orders", "body")];

        var act = () => publisher.PublishAsync(messages, null,
            (ok, _, _) => { outcomes.Add(ok); return Task.CompletedTask; },
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<MessageBatchPublishException>();
        exception.Which.FailedMessageCount.Should().Be(1);
        exception.Which.InnerException.Should().BeOfType<MessagePublishException>();
        outcomes.Should().Equal(false);
        producer.Verify(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenAfterCallbackFails_DoesNotInvokeItTwice()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer.Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        IMessageBrokerPublisher publisher = new KafkaMessageBrokerPublisher(producer.Object, AppTelemetry.Null);
        var calls = 0;
        OutboundMessage[] messages = [new("one", "orders", "body")];

        var act = () => publisher.PublishAsync(messages, null, (ok, _, _) => {
            ok.Should().BeTrue();
            calls++;
            return Task.FromException(new InvalidOperationException("callback failed"));
        }, cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<MessageBatchPublishException>();
        calls.Should().Be(1);
    }
}
