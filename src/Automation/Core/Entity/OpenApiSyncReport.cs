namespace Anarchy.Toolkit.Automation.Core.Entity;

/// <summary>
/// The outcome of synchronizing mock server expectations with an OpenAPI specification (see
/// <see cref="Anarchy.Toolkit.Automation.Core.Service.MockServer.OpenApiExpectationSync"/>).
/// </summary>
/// <param name="Added">Expectations generated for endpoints that had none.</param>
/// <param name="Notices">Differences found between the expectations and the specification, left for the developer.</param>
/// <param name="Warnings">Problems reading the specification or the expectation files.</param>
public sealed record OpenApiSyncReport(
    IReadOnlyList<OpenApiSyncAddition> Added,
    IReadOnlyList<OpenApiSyncNotice> Notices,
    IReadOnlyList<string> Warnings);

/// <summary>
/// An expectation generated for an endpoint of the specification.
/// </summary>
/// <param name="Id">The expectation id (the operation id, or one derived from the method and path).</param>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path template.</param>
/// <param name="StatusCode">The status code it answers with.</param>
/// <param name="File">The file it was (or, in a dry run, would be) appended to.</param>
public sealed record OpenApiSyncAddition(string Id, string Method, string Path, int StatusCode, string File);

/// <summary>
/// What kind of difference an <see cref="OpenApiSyncNotice"/> reports.
/// </summary>
public enum OpenApiSyncNoticeKind
{
    /// <summary>An expectation's endpoint is not in the specification (removed, renamed, or never there).</summary>
    NotInSpecification,

    /// <summary>An expectation has the id of an operation whose method or path is now different.</summary>
    EndpointChanged,

    /// <summary>An expectation answers with a status code the operation does not document.</summary>
    UndocumentedStatusCode,
}

/// <summary>
/// A difference between an existing expectation and the specification. The synchronizer never acts on it.
/// </summary>
/// <param name="Kind">What kind of difference.</param>
/// <param name="Expectation">The expectation's id, or its method and path when it has none.</param>
/// <param name="File">The file holding the expectation.</param>
/// <param name="Message">A human-readable description.</param>
public sealed record OpenApiSyncNotice(OpenApiSyncNoticeKind Kind, string Expectation, string File, string Message);
