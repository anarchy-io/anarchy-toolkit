using System.Dynamic;

namespace Anarchy.Toolkit.Core.Extension;

/// <summary>
/// General-purpose extension and helper methods for arbitrary objects.
/// </summary>
public static class ObjExtension
{
    /// <param name="obj">The extended object whose public properties form the base of the merge.</param>
    extension(object obj)
    {
        /// <summary>
        /// Shallow-merges the public instance properties of this object and <paramref name="source"/> into a new
        /// dynamic object.
        /// </summary>
        /// <param name="source">The object whose properties are copied on top; on name clashes its values win.</param>
        /// <returns>
        /// A new <see cref="ExpandoObject"/> (typed as <see langword="dynamic"/>) containing the properties of both
        /// objects. Property values are copied by reference.
        /// </returns>
        /// <example>
        /// <code language="csharp">
        /// dynamic merged = new { Id = 1, Name = "a" }.Merge(new { Name = "b", Extra = true });
        /// // merged.Id == 1, merged.Name == "b", merged.Extra == true
        /// </code>
        /// </example>
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

    /// <summary>
    /// Invokes <paramref name="func"/> and swallows any exception it throws.
    /// </summary>
    /// <typeparam name="T">The reference type produced by <paramref name="func"/>.</typeparam>
    /// <param name="func">The factory to invoke.</param>
    /// <returns>The result of <paramref name="func"/>, or <see langword="null"/> if it threw any exception.</returns>
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

    /// <summary>
    /// Invokes <paramref name="func"/> and returns <see langword="null"/> only when it throws
    /// <typeparamref name="TException"/>; other exceptions propagate.
    /// </summary>
    /// <typeparam name="T">The reference type produced by <paramref name="func"/>.</typeparam>
    /// <typeparam name="TException">The exception type (or base type) to swallow.</typeparam>
    /// <param name="func">The factory to invoke.</param>
    /// <returns>
    /// The result of <paramref name="func"/>, or <see langword="null"/> if it threw <typeparamref name="TException"/>.
    /// </returns>
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
