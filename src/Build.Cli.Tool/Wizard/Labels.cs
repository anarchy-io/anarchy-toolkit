using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Tool.Wizard;

/// <summary>
/// How the tool names application kinds and layering modes in prompts and tables.
/// </summary>
public static class Labels
{
    /// <summary>
    /// The short name of <paramref name="kind"/>, as used in sentences (<c>API</c>, <c>job</c>, <c>UI</c>).
    /// </summary>
    public static string Of(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "API",
        ApplicationKind.Job => "job",
        ApplicationKind.Ui => "UI",
        _ => kind.ToString()
    };

    /// <summary>
    /// A selection-list label for <paramref name="kind"/>, naming the toolkit package it builds on.
    /// </summary>
    public static string DescribeKind(ApplicationKind kind) => kind switch {
        ApplicationKind.Api => "API - a REST API on Anarchy.Toolkit.Web",
        ApplicationKind.Job => "Job - a background job or AWS Lambda function on Anarchy.Toolkit.Hosting",
        ApplicationKind.Ui => "UI - a Blazor back-office UI on Anarchy.Toolkit.Web.Ui",
        _ => kind.ToString()
    };

    /// <summary>
    /// A selection-list label for <paramref name="mode"/>, describing where projects are placed.
    /// </summary>
    public static string DescribeMode(LayeringMode mode) => mode switch {
        LayeringMode.Folder => "Folders - src/Library/<Layer> for libraries; src/Api, src/Jobs and src/Ui for applications",
        LayeringMode.Prefix => "Name prefixes - src/<Layer>.<Name> for every project",
        _ => mode.ToString()
    };
}
