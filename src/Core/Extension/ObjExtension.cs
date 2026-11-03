using System.Dynamic;

namespace Anarchy.Toolkit.Core.Extension;

public static class ObjExtension
{
    // TODO(generalize): CalledFrom moved to Observability
    // The original ObjExtension.CalledFrom([CallerMemberName]) returned a CallSiteEntity,
    // which lives in the Anarchy.Toolkit.Observability package. It is intentionally
    // omitted here to keep Primitives free of that dependency.

    extension(object obj)
    {
        public dynamic Merge(object source)
        {
            var result = new ExpandoObject() as IDictionary<string, object?>;

            foreach (var property in obj.GetType().GetProperties())
            {
                result[property.Name] = property.GetValue(obj);
            }

            foreach (var property in source.GetType().GetProperties())
            {
                result[property.Name] = property.GetValue(source);
            }

            return result;
        }
    }

    public static T? TryOrNull<T>(Func<T> func) where T : class
    {
        try
        {
            return func();
        }
        catch
        {
            return null;
        }
    }

    public static T? TryOrNull<T, TException>(Func<T> func)
        where T : class
        where TException : Exception
    {
        try
        {
            return func();
        }
        catch (TException)
        {
            return null;
        }
    }
}
