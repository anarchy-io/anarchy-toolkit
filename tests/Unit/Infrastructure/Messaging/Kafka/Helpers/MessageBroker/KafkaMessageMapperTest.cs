using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker.Exceptions;
using Anarchy.Toolkit.Testing.Kafka.Fixture;
using AwesomeAssertions;
using Confluent.Kafka;
using System.Text;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.Kafka.Helpers.MessageBroker;

public class KafkaMessageMapperTest
{
    [Fact]
    public void FromKafka_WhenPublishedByToolkit_PreservesMessageAndAttributes()
    {
        var message = new OutboundMessage("id", "orders", "héllo", "key", attributes: new Dictionary<string, string> { ["Source"] = "service" });
        var record = KafkaMessageFixture.FromOutbound(message, partition: 2, offset: 19);

        var result = KafkaMessageMapper.FromKafka(record);

        result.Id.Should().Be("id");
        result.BrokerReference.Should().Be("orders:2:19");
        result.Source.Should().Be("orders");
        result.Message.Should().Be("héllo");
        result.PartitionKey.Should().Be("key");
        result.Attributes.Should().BeEquivalentTo(message.Attributes);
    }

    [Fact]
    public void FromKafka_WhenNoIdHeader_UsesRecordLocation()
    {
        var record = new ConsumeResult<string, string> {
            Topic = "orders",
            Partition = new Partition(1),
            Offset = new Offset(7),
            Message = new Message<string, string> { Value = "payload" }
        };

        KafkaMessageMapper.FromKafka(record).Id.Should().Be("orders:1:7");
    }

    [Fact]
    public void ToKafka_WhenAttributeIsReserved_Throws()
    {
        var message = new OutboundMessage("id", "orders", "body", attributes: new Dictionary<string, string> { [KafkaMessageMapper.IdHeader] = "other" });
        var act = () => KafkaMessageMapper.ToKafka(message);
        var exception = act.Should().ThrowExactly<ReservedKafkaHeaderException>().Which;
        exception.HeaderName.Should().Be(KafkaMessageMapper.IdHeader);
        exception.ParamName.Should().Be("message");
        exception.Should().BeAssignableTo<ArgumentException>();
    }

    [Fact]
    public void ToKafka_WhenAttributeIsNull_ThrowsSpecificNullArgumentException()
    {
        var attributes = new Dictionary<string, string> { ["missing"] = null! };
        var message = new OutboundMessage("id", "orders", "body", attributes: attributes);
        var act = () => KafkaMessageMapper.ToKafka(message);

        var exception = act.Should().ThrowExactly<NullKafkaAttributeException>().Which;
        exception.HeaderName.Should().Be("missing");
        exception.Should().BeAssignableTo<ArgumentNullException>();
    }

    [Fact]
    public void FromKafka_WhenHeadersAreDuplicated_Throws()
    {
        var record = KafkaMessageFixture.FromOutbound(new OutboundMessage("id", "orders", "body"));
        record.Message.Headers.Add(KafkaMessageMapper.IdHeader, Encoding.UTF8.GetBytes("other"));
        var act = () => KafkaMessageMapper.FromKafka(record);
        var exception = act.Should().ThrowExactly<DuplicateKafkaHeaderException>().Which;
        exception.HeaderName.Should().Be(KafkaMessageMapper.IdHeader);
        exception.Should().BeAssignableTo<KafkaHeaderMappingException>();
        exception.Should().BeAssignableTo<FormatException>();
    }

    [Theory]
    [InlineData("bad")]
    [InlineData(KafkaMessageMapper.IdHeader)]
    [InlineData(KafkaMessageMapper.DeduplicationIdHeader)]
    public void FromKafka_WhenHeaderIsNotUtf8_Throws(string headerName)
    {
        var record = KafkaMessageFixture.FromOutbound(new OutboundMessage("id", "orders", "body"));
        record.Message.Headers.Remove(headerName);
        record.Message.Headers.Add(headerName, [0xff]);
        var act = () => KafkaMessageMapper.FromKafka(record);
        var exception = act.Should().ThrowExactly<InvalidKafkaHeaderEncodingException>().Which;
        exception.HeaderName.Should().Be(headerName);
        exception.InnerException.Should().BeOfType<DecoderFallbackException>();
        exception.Should().BeAssignableTo<KafkaHeaderMappingException>();
        exception.Should().BeAssignableTo<FormatException>();
    }

    [Fact]
    public void FromKafka_WhenHeaderValueIsNull_ThrowsMappingException()
    {
        var record = KafkaMessageFixture.FromOutbound(new OutboundMessage("id", "orders", "body"));
        record.Message.Headers.Add("missing", null!);
        var act = () => KafkaMessageMapper.FromKafka(record);

        var exception = act.Should().ThrowExactly<NullKafkaHeaderException>().Which;
        exception.HeaderName.Should().Be("missing");
        exception.Should().BeAssignableTo<KafkaHeaderMappingException>();
        exception.Should().BeAssignableTo<FormatException>();
    }

    [Fact]
    public void FromKafka_WhenPartitionEnd_ThrowsSpecificArgumentException()
    {
        var record = new ConsumeResult<string, string> { IsPartitionEOF = true };
        var act = () => KafkaMessageMapper.FromKafka(record);

        var exception = act.Should().ThrowExactly<KafkaPartitionEndException>().Which;
        exception.ParamName.Should().Be("result");
        exception.Should().BeAssignableTo<ArgumentException>();
    }

    [Fact]
    public void Mapper_WhenArgumentsAreNull_ThrowsSpecificNullArgumentException()
    {
        var outbound = () => KafkaMessageMapper.ToKafka(null!);
        var inbound = () => KafkaMessageMapper.FromKafka(null!);

        outbound.Should().ThrowExactly<KafkaNullArgumentException>().Which.ParamName.Should().Be("message");
        var exception = inbound.Should().ThrowExactly<KafkaNullArgumentException>().Which;
        exception.ParamName.Should().Be("result");
        exception.Should().BeAssignableTo<ArgumentNullException>();
    }

    [Fact]
    public void Fixture_WhenMessageIsNull_ThrowsSpecificNullArgumentException()
    {
        var act = () => KafkaMessageFixture.FromOutbound(null!);

        var exception = act.Should().ThrowExactly<KafkaNullArgumentException>().Which;
        exception.ParamName.Should().Be("message");
        exception.Should().BeAssignableTo<ArgumentNullException>();
    }
}
