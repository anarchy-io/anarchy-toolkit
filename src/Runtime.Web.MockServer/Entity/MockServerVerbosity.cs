namespace Anarchy.Toolkit.Runtime.Web.MockServer.Entity;

/// <summary>
/// How much the mock server writes to the console, following the <c>dotnet</c> verbosity levels.
/// </summary>
/// <remarks>The <c>MOCKSERVER_READY</c> line is always written, whatever the verbosity.</remarks>
public enum MockServerVerbosity
{
    /// <summary>Only errors.</summary>
    Quiet,

    /// <summary>A one-line startup summary, unmatched requests, warnings and errors.</summary>
    Minimal,

    /// <summary>The startup banner and one line per request.</summary>
    Normal,

    /// <summary>Also request headers and bodies, and which expectation answered.</summary>
    Detailed,

    /// <summary>Also response bodies, control requests and ASP.NET Core logs.</summary>
    Diagnostic,
}
