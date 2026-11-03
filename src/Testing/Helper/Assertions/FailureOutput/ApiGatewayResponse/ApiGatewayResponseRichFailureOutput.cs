using Amazon.Lambda.APIGatewayEvents;
using Spectre.Console;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.Helper.Assertions.FailureOutput.ApiGatewayResponse;

public static class ApiGatewayResponseRichFailureOutput
{
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
