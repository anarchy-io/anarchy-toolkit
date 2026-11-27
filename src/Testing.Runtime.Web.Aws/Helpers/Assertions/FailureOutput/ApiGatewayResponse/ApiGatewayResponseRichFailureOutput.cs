using Amazon.Lambda.APIGatewayEvents;
using Spectre.Console;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.Runtime.Web.Aws.Helpers.Assertions.FailureOutput.ApiGatewayResponse;

/// <summary>
/// Formats an <see cref="APIGatewayProxyResponse"/> as a Spectre.Console table (with ANSI markup) for assertion failure messages.
/// </summary>
public static class ApiGatewayResponseRichFailureOutput
{
    /// <summary>
    /// Renders the status code, headers and pretty-printed body into a rounded table, captured as a string rather than written to the console.
    /// </summary>
    /// <param name="response">The response to format.</param>
    /// <returns>The rendered table.</returns>
    public static string Format(APIGatewayProxyResponse response)
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Red);
        table.AddColumn("[yellow]Section[/]");
        table.AddColumn("[yellow]Value[/]");

        table.AddRow("StatusCode", $"[red]{response.StatusCode}[/]");

        var headers = response.Headers is { Count: > 0 }
            ? JsonSerializer.Serialize(response.Headers, new JsonSerializerOptions { WriteIndented = true })
            : "<none>";
        table.AddRow("Headers", headers);

        var body = PrettyPrintBody(response.Body);
        table.AddRow("Body", body);

        // Render to string, not console
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) });
        console.Write(table);

        return writer.ToString();
    }

    private static string PrettyPrintBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "<null>";

        try
        {
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return body;
        }
    }
}
