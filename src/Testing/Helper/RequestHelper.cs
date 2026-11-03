using System.Text;
using System.Text.Json;

namespace Anarchy.Toolkit.Testing.Helper;

public static class RequestHelper
{
    public static StringContent CreateJsonContent<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
