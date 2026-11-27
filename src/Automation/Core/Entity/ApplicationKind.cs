namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// The kinds of application a solution can be bootstrapped with.
/// </summary>
public enum ApplicationKind
{
    /// <summary>
    /// A REST API on <c>Anarchy.Toolkit.Runtime.Web</c>.
    /// </summary>
    Api,

    /// <summary>
    /// A job on <c>Anarchy.Toolkit.Runtime</c> (an AWS Lambda function on <c>Anarchy.Toolkit.Runtime.Aws</c>).
    /// </summary>
    Job,

    /// <summary>
    /// An interactive-server Blazor UI on <c>Anarchy.Toolkit.Runtime.Web.Ui</c>.
    /// </summary>
    Ui
}
