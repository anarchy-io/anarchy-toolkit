namespace Anarchy.Toolkit.Infrastructure.Http.Entity.Http.Common;

public class HttpHeader
{
    public HttpHeader(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string Value { get; }
}
