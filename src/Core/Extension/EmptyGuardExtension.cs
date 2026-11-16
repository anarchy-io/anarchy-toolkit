using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace Anarchy.Toolkit.Core.Extension;

/// <summary>
/// Fluent guard clauses for argument validation.
/// </summary>
/// <example>
/// <code language="csharp">
/// public Customer(string name, int age)
/// {
///     Name = name.CheckNotEmpty(nameof(name));
///     (age >= 18).CheckTrue(nameof(age), "Customer must be an adult");
/// }
/// </code>
/// </example>
public static class EmptyGuardExtension
{
    /// <param name="obj">The extended string; may be <see langword="null"/>.</param>
    extension([NotNullWhen(false)] string? obj)
    {
        /// <summary>
        /// Fluent equivalent of <see cref="string.IsNullOrEmpty(string)"/>.
        /// </summary>
        /// <returns><see langword="true"/> when the string is <see langword="null"/> or empty.</returns>
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(obj);
        }

        /// <summary>
        /// Ensures the string is not <see langword="null"/>, empty or whitespace and returns it, allowing inline use in
        /// assignments.
        /// </summary>
        /// <param name="paramName">The argument name reported in the exception.</param>
        /// <param name="message">Optional custom exception message.</param>
        /// <returns>The original, validated string.</returns>
        /// <exception cref="EmptyArgumentException">The string is null, empty or whitespace.</exception>
        public string CheckNotEmpty(string paramName, string? message = null)
        {
            return string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)
                ? throw new EmptyArgumentException(paramName, message)
                : obj;
        }
    }

    /// <summary>
    /// Throws when the given condition is <see langword="false"/>.
    /// </summary>
    /// <param name="thisBoolean">The condition that must hold.</param>
    /// <param name="paramName">The argument name reported in the exception.</param>
    /// <param name="message">Optional custom exception message.</param>
    /// <exception cref="ArgumentCheckException"><paramref name="thisBoolean"/> is <see langword="false"/>.</exception>
    public static void CheckTrue(this bool thisBoolean, string paramName = "", string? message = null)
    {
        if (!thisBoolean)
            throw new ArgumentCheckException(paramName, message);
    }
}
