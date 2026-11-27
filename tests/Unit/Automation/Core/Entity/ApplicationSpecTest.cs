using Anarchy.Toolkit.Automation.Core.Entity;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Automation.Core.Entity;

public class ApplicationSpecTest
{
    [Theory]
    [InlineData(ApplicationKind.Api, false, false, "Anarchy.Toolkit.Runtime.Web")]
    [InlineData(ApplicationKind.Api, true, false, "Anarchy.Toolkit.Runtime.Web.Aws")]
    [InlineData(ApplicationKind.Ui, false, false, "Anarchy.Toolkit.Runtime.Web.Ui")]
    [InlineData(ApplicationKind.Job, false, false, "Anarchy.Toolkit.Runtime")]
    [InlineData(ApplicationKind.Job, false, true, "Anarchy.Toolkit.Runtime.Aws")]
    public void PackageId_ReferencesAnAwsPackageOnlyForLambdaEntryPoints(ApplicationKind kind, bool apiLambda,
                                                                         bool eventLambda, string expected)
    {
        var app = new ApplicationSpec(kind, "Orders") { ApiLambda = apiLambda, EventLambda = eventLambda };

        app.PackageId.Should().Be(expected);
    }

    [Theory]
    [InlineData(ApplicationKind.Api, true, new[] { "Anarchy.Toolkit.Runtime.Web", "Anarchy.Toolkit.Runtime.Web.Swagger" })]
    [InlineData(ApplicationKind.Api, false, new[] { "Anarchy.Toolkit.Runtime.Web" })]
    [InlineData(ApplicationKind.Ui, true, new[] { "Anarchy.Toolkit.Runtime.Web.Ui" })]
    [InlineData(ApplicationKind.Job, true, new[] { "Anarchy.Toolkit.Runtime" })]
    public void PackageIds_AddTheSwaggerPackageOnlyForApisWithSwagger(ApplicationKind kind, bool swagger, string[] expected)
    {
        var app = new ApplicationSpec(kind, "Orders") { Swagger = swagger };

        app.PackageIds.Should().Equal(expected);
    }
}
