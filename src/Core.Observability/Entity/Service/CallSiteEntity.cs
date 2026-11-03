using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Observability.Entity.Service;

public record CallSiteEntity(string ClassName, string MethodName)
{
    public string ToMetricPrefix()
    {
        return $"{ClassName.ToSnakeCase()}.{MethodName.ToSnakeCase()}";
    }

    public string ToLogPrefix()
    {
        return $"[{ClassName} → {MethodName}]";
    }
}
