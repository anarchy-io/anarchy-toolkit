using Anarchy.Toolkit.Web.Presentation.Converter.Json;
using AwesomeAssertions;
using System.ComponentModel;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Unit.Web.Presentation.Converter.Json;

public class DescribableEnumJsonConverterTest
{
    private enum TestEnum
    {
        [Description("first_value")] First = 1,

        [Description("second_value")] Second = 2,

        // No DescriptionAttribute -> should fall back to name
        Third = 3
    }

    private sealed class TestModel
    {
        public TestEnum Value { get; init; }

        public TestEnum? NullableValue { get; init; }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        options.Converters.Add(new DescribableEnumJsonConverter());

        return options;
    }

    [Fact]
    public void Serialize_NonNullableEnum_WithDescription_WritesDescription()
    {
        // Arrange
        var model = new TestModel { Value = TestEnum.First };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(model, options);

        // Assert
        json.Should().Contain("\"value\":\"first_value\"");
    }

    [Fact]
    public void Serialize_NonNullableEnum_WithoutDescription_FallsBackToName()
    {
        // Arrange
        var model = new TestModel { Value = TestEnum.Third };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(model, options);

        // Assert
        json.Should().Contain("\"value\":\"Third\"");
    }

    [Fact]
    public void Serialize_NullableEnum_Null_WritesJsonNull()
    {
        // Arrange
        var model = new TestModel { NullableValue = null };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(model, options);

        // Assert
        json.Should().Contain("\"nullableValue\":null");
    }

    [Fact]
    public void Serialize_NullableEnum_WithDescription_WritesDescription()
    {
        // Arrange
        var model = new TestModel { NullableValue = TestEnum.Second };
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(model, options);

        // Assert
        json.Should().Contain("\"nullableValue\":\"second_value\"");
    }

    [Fact]
    public void Deserialize_NonNullableEnum_WithValidDescription_ReturnsEnumValue()
    {
        // Arrange
        const string json = "{\"value\":\"second_value\"}";
        var options = CreateOptions();

        // Act
        var model = JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        model.Should().NotBeNull();
        model!.Value.Should().Be(TestEnum.Second);
    }

    [Fact]
    public void Deserialize_NullableEnum_WithValidDescription_ReturnsEnumValue()
    {
        // Arrange
        const string json = "{\"nullableValue\":\"first_value\"}";
        var options = CreateOptions();

        // Act
        var model = JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        model.Should().NotBeNull();
        model!.NullableValue.Should().Be(TestEnum.First);
    }

    [Fact]
    public void Deserialize_NonNullableEnum_WithInvalidDescription_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"value\":\"does_not_exist\"}";
        var options = CreateOptions();

        // Act
        Action act = () => JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage("\"does_not_exist\" is not a valid TestEnum value.*");
    }

    [Fact]
    public void Deserialize_NullableEnum_WithInvalidDescription_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"nullableValue\":\"does_not_exist\"}";
        var options = CreateOptions();

        // Act
        Action act = () => JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage("\"does_not_exist\" is not a valid TestEnum value.*");
    }

    [Fact]
    public void Deserialize_NonNullableEnum_WithEmptyString_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"value\":\"\"}";
        var options = CreateOptions();

        // Act
        Action act = () => JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage("Cannot convert a null or empty JSON value to TestEnum.*");
    }

    [Fact]
    public void Deserialize_NullableEnum_WithEmptyString_ReturnsNull()
    {
        // Arrange
        const string json = "{\"nullableValue\":\"\"}";
        var options = CreateOptions();

        // Act
        var model = JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        model.Should().NotBeNull();
        model!.NullableValue.Should().BeNull();
    }

    [Fact]
    public void Deserialize_NonStringToken_ForEnum_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"value\":123}";
        var options = CreateOptions();

        // Act
        Action act = () => JsonSerializer.Deserialize<TestModel>(json, options);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage("Unexpected token Number when parsing TestEnum.*");
    }
}
