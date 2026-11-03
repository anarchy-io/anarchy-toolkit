using Anarchy.Toolkit.Core.Observability.Service.Observability;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Observability.Core.Service.Observability;

public class MeasurerScopeTests
{
    [Fact]
    public void CreatingMeasurerScope_ShouldAddTagToCurrent()
    {
        var tag = "success:true";

        using var _ = MeasurerBase.BeginScope(tag);

        MeasurerBase.ScopedTags.Should().Contain(tag);
    }

    [Fact]
    public void DisposingMeasurerScope_ShouldRemoveTagFromCurrent()
    {
        var tag = "success:true";

        var handle = MeasurerBase.BeginScope(tag);

        handle.Dispose();

        MeasurerBase.ScopedTags.Should().NotContain(tag);
    }

    [Fact]
    public void MultipleMeasurerScope_ShouldAllBeInCurrent()
    {
        var tag1 = "success:true";
        var tag2 = "success:false";

        using var _ = MeasurerBase.BeginScope(tag1);
        using var __ = MeasurerBase.BeginScope(tag2);

        MeasurerBase.ScopedTags.Should().Contain(tag1);
        MeasurerBase.ScopedTags.Should().Contain(tag2);
    }

    [Fact]
    public void MultipleMeasurerScope_DisposeOne_ShouldRemoveOnlyThatTag()
    {
        var tag1 = "success:true";
        var tag2 = "success:false";
        var tag3 = "mytag:value";

        using var _ = MeasurerBase.BeginScope(tag1);
        using var handle = MeasurerBase.BeginScope(tag2);
        using var __ = MeasurerBase.BeginScope(tag3);

        handle.Dispose();

        MeasurerBase.ScopedTags.Should().Contain(tag1);
        MeasurerBase.ScopedTags.Should().NotContain(tag2);
        MeasurerBase.ScopedTags.Should().Contain(tag3);
    }

    [Fact]
    public void MultipleTagsWithSameValue_DisposeOne_ShouldRemoveOnlyOneTag()
    {
        var tag = "success:true";

        using var _ = MeasurerBase.BeginScope(tag);
        using var handle = MeasurerBase.BeginScope(tag);

        handle.Dispose();

        MeasurerBase.ScopedTags.Should().ContainSingle().Which.Should().Be(tag);
    }

    [Fact]
    public void NullTag_ShouldThrowArgumentNullException()
    {
        Action act = () => MeasurerBase.BeginScope(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tag");
    }
}
