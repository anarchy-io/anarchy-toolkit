namespace Anarchy.Toolkit.Core.Health.Entity.Attribute;

/// <summary>
/// Indicates an application's runbook URI.
/// </summary>
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
    public ApplicationRunbookUriAttribute(string uri)
    {
        Uri = uri;
    }
}
