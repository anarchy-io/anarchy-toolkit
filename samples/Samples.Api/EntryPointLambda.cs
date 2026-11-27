using Anarchy.Toolkit.Bootstrap.Aws.Extensions.Microsoft.Extensions.Configuration;
using Anarchy.Toolkit.Runtime.Web.Aws;
using Anarchy.Toolkit.Samples.Api.Composition;

namespace Anarchy.Toolkit.Samples.Api;

/// <summary>
/// AWS Lambda handler (<c>Anarchy.Toolkit.Samples.Api::Anarchy.Toolkit.Samples.Api.EntryPointLambda::FunctionHandlerAsync</c>).
/// Reading the Parameter Store is this application's choice, not the toolkit's.
/// </summary>
public sealed class EntryPointLambda : AwsWebAppEntryPointBase<SampleApiStartup>
{
    protected override void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
    {
        builder.AddAwsSystemsManagerParameterStore(context.HostingEnvironment, "/samples/",
            appNamePrefix: "Anarchy.Toolkit.Samples.");
    }
}
