namespace Anarchy.Toolkit.Infrastructure.Http.Extension;

internal static class TypeExtension
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
