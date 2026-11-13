using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>How the tool names application kinds and layering modes.</summary>
public static class Labels
{
    public static string Of(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "API",
        ApplicationKind.Job => "job",
        ApplicationKind.Ui => "UI",
        _ => kind.ToString()
    };

    public static string DescribeKind(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "API - a REST API on Anarchy.Toolkit.Web",
        ApplicationKind.Job => "Job - a background job or AWS Lambda function on Anarchy.Toolkit.Hosting",
        ApplicationKind.Ui => "UI - a Blazor back-office UI on Anarchy.Toolkit.Web.Ui",
        _ => kind.ToString()
    };

    public static string DescribeMode(LayeringMode mode) => mode switch {
        LayeringMode.Folder => "Folders - src/Library/<Layer> for libraries; src/Api, src/Jobs and src/Ui for applications",
        LayeringMode.Prefix => "Name prefixes - src/<Layer>.<Name> for every project",
        _ => mode.ToString()
    };
}
