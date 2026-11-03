using Anarchy.Toolkit.Core.Observability.Service.Observability;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.Core.Service.Observability;

public class TagsTests
{
    [Fact]
    public void Failure_WithoutOtherTags_SuccessIsFalse()
    {
        string[] tags = Tags.Failure;
        string[] expected = ["success:false"];

        tags.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Success_WithoutOtherTags_SuccessIsTrue()
    {
        string[] tags = Tags.Success;
        string[] expected = ["success:true"];

        tags.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Success_WithOneTag_SuccessIsTrueAndContainsTag()
    {
        var tagValue = Guid.NewGuid().ToString();
        string[] expected = ["success:true", $"{tagValue}:2"];
        string[] tags = Tags.Success.WithTag(tagValue, 2);

        tags.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Failure_With2Tags_ContainsBoth()
    {
        string[] tags = Tags.Failure.WithTag("two", 2).WithTag("three", "value of 3");
        string[] expected = ["success:false", "two:2", "three:value of 3"];

        tags.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WithOutcomeSuccess_ContainsOutcomeSuccessTag()
    {
        string[] tags = Tags.Success.WithOutcomeSuccess();
        string[] expected = ["success:true", "outcome:success"];

        tags.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WithOutcome_ContainsOutcomeTag()
    {
        var tagValue = Guid.NewGuid().ToString();
        string[] tags = Tags.Success.WithOutcome(tagValue);
        string[] expected = ["success:true", $"outcome:{tagValue}"];

        tags.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WithOutcomeException_ContainsOutcomeTag()
    {
        string[] tags = Tags.Failure.WithOutcome(new InvalidOperationException());
        string[] expected = ["success:false", "outcome:InvalidOperationException"];

        tags.Should().BeEquivalentTo(expected);
    }
}
