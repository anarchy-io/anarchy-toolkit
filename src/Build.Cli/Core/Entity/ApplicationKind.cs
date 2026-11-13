namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>The kinds of application a solution can be bootstrapped with.</summary>
public enum ApplicationKind
{
    /// <summary>A REST API on <c>Anarchy.Toolkit.Web</c>.</summary>
    Api,

    /// <summary>A job (e.g. an AWS Lambda function) on <c>Anarchy.Toolkit.Hosting</c>.</summary>
    Job,

    /// <summary>An interactive-server Blazor UI on <c>Anarchy.Toolkit.Web.Ui</c>.</summary>
    Ui
}
