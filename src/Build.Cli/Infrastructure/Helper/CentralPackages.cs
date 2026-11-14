using System.Xml.Linq;

namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// NuGet central package management: a <c>Directory.Packages.props</c> with <c>ManagePackageVersionsCentrally</c>
/// set, where projects declare <c>PackageReference</c>s without versions and the props file holds the
/// <c>PackageVersion</c>s.
/// </summary>
public static class CentralPackages
{
    /// <summary>
    /// The file name of the central package versions, <c>Directory.Packages.props</c>.
    /// </summary>
    public const string FileName = "Directory.Packages.props";

    /// <summary>
    /// A props file enabling central package management for every package listed, ordered by package ID.
    /// </summary>
    public static string Render(IReadOnlyDictionary<string, string> versions) => $"""
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>
          <ItemGroup>
        {string.Concat(versions.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"    <PackageVersion Include=\"{pair.Key}\" Version=\"{pair.Value}\" />\n"))}  </ItemGroup>
        </Project>

        """;

    /// <summary>
    /// The <c>Directory.Packages.props</c> a project in <paramref name="directory"/> would import (the nearest one,
    /// searching upwards as MSBuild does), if it enables central package management; otherwise null. When the props
    /// file does not set <c>ManagePackageVersionsCentrally</c>, the nearest <c>Directory.Build.props</c> decides.
    /// File names match case-insensitively, and unreadable XML counts as not set.
    /// </summary>
    public static string? Find(string directory)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(directory)); current is not null; current = current.Parent)
        {
            if (!current.Exists) continue;
            var props = FileHelper.ExistingEntryPath(current.FullName, FileName);
            if (!File.Exists(props)) continue;
            var buildProps = FindUpwards(current.FullName, "Directory.Build.props");
            return IsEnabled(props) ?? (buildProps is null ? null : IsEnabled(buildProps)) ?? false ? props : null;
        }
        return null;
    }

    /// <summary>
    /// Adds a <c>PackageVersion</c> unless the props file already pins the package; existing versions are kept. The
    /// entry follows the last <c>PackageVersion</c>, or goes in a new <c>ItemGroup</c>, preserving the file's formatting.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the props file has no root element.</exception>
    public static void Ensure(string propsPath, string packageId, string version)
    {
        var document = XDocument.Load(propsPath, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException($"{propsPath} has no root element.");
        var ns = root.Name.Namespace;
        var versions = root.Descendants(ns + "PackageVersion").ToList();
        if (versions.Any(element => string.Equals((string?)element.Attribute("Include"), packageId, StringComparison.OrdinalIgnoreCase)))
            return;

        var entry = new XElement(ns + "PackageVersion", new XAttribute("Include", packageId), new XAttribute("Version", version));
        if (versions.LastOrDefault() is { } last)
        {
            var indent = last.PreviousNode is XText text ? text.Value : "\n    ";
            last.AddAfterSelf(new XText(indent), entry);
        }
        else
        {
            var group = new XElement(ns + "ItemGroup", new XText("\n    "), entry, new XText("\n  "));
            if (root.LastNode is XText closing) closing.AddBeforeSelf(new XText("\n  "), group);
            else root.Add(new XText("\n  "), group, new XText("\n"));
        }

        using var writer = System.Xml.XmlWriter.Create(propsPath, new System.Xml.XmlWriterSettings {
            OmitXmlDeclaration = document.Declaration is null,
            Encoding = new System.Text.UTF8Encoding(false)
        });
        document.Save(writer);
    }

    private static bool? IsEnabled(string path)
    {
        try
        {
            var value = XDocument.Load(path).Descendants()
                .LastOrDefault(element => element.Name.LocalName == "ManagePackageVersionsCentrally")?.Value.Trim();
            return value is null ? null : string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string? FindUpwards(string directory, string name)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            var path = FileHelper.ExistingEntryPath(current.FullName, name);
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
