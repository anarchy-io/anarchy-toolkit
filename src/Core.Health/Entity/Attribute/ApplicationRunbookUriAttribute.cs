namespace Anarchy.Toolkit.Core.Health.Entity.Attribute;

/// <summary>
/// Indicates an application's runbook URI.
/// </summary>
/// <remarks>
/// Apply at assembly level so the URI can be surfaced through <see cref="Anarchy.Toolkit.Core.Health.Service.Health.IBuildMetadata.RunbookUri"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// [assembly: ApplicationRunbookUri("https://wiki.example.com/runbooks/my-service")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public class ApplicationRunbookUriAttribute : System.Attribute
{
    /// <summary>
    /// The application's runbook URI.
    /// </summary>
    public string Uri { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ApplicationRunbookUriAttribute"/>.
    /// </summary>
    /// <param name="uri">The runbook URI.</param>
    public ApplicationRunbookUriAttribute(string uri)
    {
        Uri = uri;
    }
}
