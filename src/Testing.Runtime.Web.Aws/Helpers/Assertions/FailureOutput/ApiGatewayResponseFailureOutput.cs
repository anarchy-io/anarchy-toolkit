using Amazon.Lambda.APIGatewayEvents;
using Anarchy.Toolkit.Testing.Runtime.Web.Aws.Helpers.Assertions.FailureOutput.ApiGatewayResponse;
using Spectre.Console;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Helpers.Assertions.FailureOutput;

/// <summary>
/// Chooses between rich and plain formatting when dumping an <see cref="APIGatewayProxyResponse"/> in failure messages.
/// </summary>
public static class ApiGatewayResponseFailureOutput
{
    /// <summary>
    /// Formats <paramref name="response"/> using <see cref="ApiGatewayResponseRichFailureOutput"/> when
    /// <paramref name="allowRichOutput"/> is <see langword="true"/> and the console supports ANSI and is interactive;
    /// otherwise uses <see cref="ApiGatewayResponsePlainFailureOutput"/>.
    /// </summary>
    /// <param name="response">The response to format.</param>
    /// <param name="allowRichOutput">Whether rich output may be used.</param>
    /// <returns>The formatted dump.</returns>
    public static string Format(APIGatewayProxyResponse response, bool allowRichOutput = true)
    {
        return allowRichOutput && AnsiConsole.Profile.Capabilities.Ansi && AnsiConsole.Profile.Capabilities.Interactive
            ? ApiGatewayResponseRichFailureOutput.Format(response)
            : ApiGatewayResponsePlainFailureOutput.Format(response);
    }
}
