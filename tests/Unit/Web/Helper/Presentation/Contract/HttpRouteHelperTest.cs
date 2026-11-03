using Anarchy.Toolkit.Web.Helper.Presentation.Contract;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Web.Helper.Presentation.Contract;

public class HttpRouteHelperTest
{
    [Fact]
    public void Build_WhenRouteHasTypedTemplateAndPositionalParameter_ReplacesTemplate()
    {
        var route = HttpRouteHelper.FromTemplate("registry/{registryProductId:long}", "product", 42);

        route.Should().Be("product/registry/42");
    }

    [Fact]
    public void Build_WhenRouteHasTypedTemplateAndNamedParameter_ReplacesTemplate()
    {
        var route = HttpRouteHelper.FromTemplate("catalog/{catalogProductId:long}", "product", new {
            catalogProductId = 77
        });

        route.Should().Be("product/catalog/77");
    }

    [Fact]
    public void Build_WhenRouteIsAbsolute_DoesNotApplyRouteBase()
    {
        var route = HttpRouteHelper.FromTemplate("/product/registry/{registryProductId:long}", "ignored", 10);

        route.Should().Be("/product/registry/10");
    }

    [Fact]
    public void Build_WhenNamedParameterIsMissing_ThrowsFormatException()
    {
        var action = () => HttpRouteHelper.FromTemplate("registry/{registryProductId:long}", "product", new {
            catalogProductId = 9
        });

        action.Should().Throw<FormatException>()
            .WithMessage("*expects named parameter values for: registryProductId*");
    }
}

