using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Core.Extension;

public static class EmptyGuardExtension
{
    extension([NotNullWhen(false)] string? obj)
    {
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(obj);
        }

        public string CheckNotEmpty(string paramName, string? message = null)
        {
            return string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)
                ? throw new EmptyArgumentException(paramName, message)
                : obj;
        }
    }

    public static void CheckTrue(this bool thisBoolean, string paramName = "", string? message = null)
    {
        if (!thisBoolean)
            throw new ArgumentCheckException(paramName, message);
    }
}
