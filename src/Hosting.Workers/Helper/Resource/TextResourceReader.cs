using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.Helper.Resource;

/// <summary>
/// Reads a resource identified by a path.
/// </summary>
/// <typeparam name="T">The type of the resource content.</typeparam>
public interface IResourceReader<out T>
{
    /// <summary>
    /// Reads the resource at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The resource path, relative to the reader's base path.</param>
    /// <returns>The resource content.</returns>
    T Read(string path);
}

/// <summary>
/// Reads embedded text resources from an assembly using file-like paths.
/// </summary>
/// <remarks>
/// Paths are converted to manifest resource names by replacing <c>/</c> and <c>\</c> with <c>.</c>. By default the
/// assembly name (with <c>-</c> replaced by <c>_</c>) is used as the root, matching the default resource naming of
/// the .NET SDK.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var reader = new TextResourceReader(typeof(Program), "Properties");
/// var json = reader.Read("build.meta.json"); // resource "MyApp.Properties.build.meta.json"
/// </code>
/// </example>
public class TextResourceReader : IResourceReader<string?>
{
    private readonly Assembly _assembly;
    private readonly string _basePath;

    /// <summary>
    /// Creates a reader for the assembly containing <paramref name="typeFromTargetAssembly"/>, with no base path.
    /// </summary>
    /// <param name="typeFromTargetAssembly">A type from the assembly holding the resources.</param>
    /// <param name="useAssemblyNameAsBasePath">Whether to prefix paths with the assembly name.</param>
    public TextResourceReader(Type typeFromTargetAssembly, bool useAssemblyNameAsBasePath = true)
        : this(typeFromTargetAssembly, string.Empty, useAssemblyNameAsBasePath)
    {
    }

    /// <summary>
    /// Creates a reader for the assembly containing <paramref name="typeFromTargetAssembly"/>.
    /// </summary>
    /// <param name="typeFromTargetAssembly">A type from the assembly holding the resources.</param>
    /// <param name="basePath">A folder-like path prepended to every read path.</param>
    /// <param name="useAssemblyNameAsBasePath">Whether to prefix paths with the assembly name.</param>
    public TextResourceReader(Type typeFromTargetAssembly, string basePath, bool useAssemblyNameAsBasePath = true)
        : this(typeFromTargetAssembly.GetTypeInfo().Assembly, basePath, useAssemblyNameAsBasePath)
    {
    }

    /// <summary>
    /// Creates a reader for <paramref name="targetAssembly"/>, with no base path.
    /// </summary>
    /// <param name="targetAssembly">The assembly holding the resources.</param>
    /// <param name="useAssemblyNameAsBasePath">Whether to prefix paths with the assembly name.</param>
    public TextResourceReader(Assembly targetAssembly, bool useAssemblyNameAsBasePath = true)
        : this(targetAssembly, string.Empty, useAssemblyNameAsBasePath)
    {
    }

    /// <summary>
    /// Creates a reader for <paramref name="targetAssembly"/>.
    /// </summary>
    /// <param name="targetAssembly">The assembly holding the resources.</param>
    /// <param name="basePath">A folder-like path prepended to every read path.</param>
    /// <param name="useAssemblyNameAsBasePath">Whether to prefix paths with the assembly name.</param>
    public TextResourceReader(Assembly targetAssembly, string basePath, bool useAssemblyNameAsBasePath = true)
    {
        _assembly = targetAssembly;
        _basePath = FileToResourcePath(
            Path.Combine(
                (useAssemblyNameAsBasePath
                     ? NormalizeAssemblyPath(_assembly.GetName().Name!)
                     : string.Empty),
                basePath
            )
        );
    }

    /// <summary>
    /// Reads the embedded resource at <paramref name="path"/> as text.
    /// </summary>
    /// <param name="path">The resource path relative to the base path; <see langword="null"/> reads the base path itself.</param>
    /// <returns>The resource text, or <see langword="null"/> if the resource does not exist.</returns>
    public string? Read(string? path)
    {
        string? result = null;
        var completeResourceName = FileToResourcePath(Path.Combine(_basePath, path ?? ""));

        using var stream = _assembly.GetManifestResourceStream(completeResourceName);

        if (stream == null)
        {
            return result;
        }

        var resourceStream = new StreamReader(stream);
        result = resourceStream.ReadToEnd();

        return result;
    }

    private static string NormalizeAssemblyPath(string path) =>
        path.Replace("-", "_");

    private static string FileToResourcePath(string path) =>
        path.Replace('/', '.')
             .Replace('\\', '.')
             .Trim('.');
}
