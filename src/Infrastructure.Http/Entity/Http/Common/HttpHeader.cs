namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

/// <summary>
/// A name/value pair added as a request header by
/// <see cref="Anarchy.Toolkit.Infrastructure.Http.Repository.Http.HttpRepositoryBase"/>.
/// </summary>
public class HttpHeader
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpHeader"/>.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    public HttpHeader(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>Gets the header name.</summary>
    public string Name { get; }

    /// <summary>Gets the header value.</summary>
    public string Value { get; }
}
