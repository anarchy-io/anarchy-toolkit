namespace Anarchy.Toolkit.Infrastructure.Http.Extensions.System;

internal static class TypeExtensions
{
    public static IEnumerable<Type> GetInheritanceChain(this Type? type)
    {
        while (type != null && type != typeof(object))
        {
            yield return type;
            type = type.BaseType!;
        }
    }
}
