using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Helper.Assertions.FailureOutput.ApiGatewayResponse;
using Spectre.Console;

namespace Anarchy.Toolkit.Testing.Helper.Assertions.FailureOutput;

public static class ApiGatewayResponseFailureOutput
{
    public static string Format(APIGatewayProxyResponse response, bool allowRichOutput = true)
    {
        return allowRichOutput && AnsiConsole.Profile.Capabilities.Ansi && AnsiConsole.Profile.Capabilities.Interactive
            ? ApiGatewayResponseRichFailureOutput.Format(response)
            : ApiGatewayResponsePlainFailureOutput.Format(response);
    }
}
