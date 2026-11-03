using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Tests.Unit.Core.Dummy;
using AwesomeAssertions;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Tests.Unit.Core.Helper;

public class ArrayHelperTest : TestCaseBase
{
    [Fact]
    [SuppressMessage("ReSharper", "UseCollectionExpression")]
    public void DeepClone_WhenDeepCloningAnEmptyArray_ReturnsFlyweightEmptyArray()
    {
        var result = ArrayHelper<int>.DeepClone([]);

        result.Should().BeSameAs(Array.Empty<int>());
    }

    [Fact]
    public void DeepClone_WhenDeepCloningNull_ReturnsEmptyArray()
    {
        var result = ArrayHelper<int>.DeepClone(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DeepClone_ReturnsANewClonedArrayWithPrimitiveValues()
    {
        var input = new[] { "a", "b", "c" };
        var result = ArrayHelper<string>.DeepClone(input);

        result.Should().NotBeSameAs(input);
        result.Should().HaveCount(input.Length);
        result.Should().Contain(input);
    }

    [Fact]
    public void DeepClone_WhenElementTypeDoesNotImplementIDeepCloneableEntity_ReturnsANewClonedArrayWithShallowCopiedElements()
    {
        var object1 = new object();
        var object2 = new object();
        var input = new[] { object1, object2 };
        var result = ArrayHelper<object>.DeepClone(input);

        result.Should().NotBeSameAs(input);
        result.Should().HaveCount(input.Length);
        result[0].Should().BeSameAs(object1);
        result[1].Should().BeSameAs(object2);
    }

    [Fact]
    public void DeepClone_WhenObjectsImplementIDeepCloneableInterface_ReturnsANewClonedArrayWithDeepClonedObjects()
    {
        var input = new[] {
            new DeepCloneableDummyEntity(),
            new DeepCloneableDummyEntity()
        };

        var result = ArrayHelper<DeepCloneableDummyEntity>.DeepClone(input);

        result.Should().NotBeSameAs(input);
        result.Should().HaveCount(input.Length);
        input[0].Should().BeEquivalentTo(result[0]);
        input[0].Should().NotBeSameAs(result[0]);
        input[0].Values.Should().NotBeSameAs(result[0].Values);
        input[1].Should().BeEquivalentTo(result[1]);
        input[1].Should().NotBeSameAs(result[1]);
    }
}
