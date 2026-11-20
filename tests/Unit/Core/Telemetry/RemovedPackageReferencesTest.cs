using AwesomeAssertions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Telemetry;

/// <summary>Telemetry replaced the former package: no namespace or package reference to it may come back.</summary>
public partial class RemovedPackageReferencesTest
{
    private static readonly string[] CheckedDirectories = ["src", "tests", "samples", "build"];
    private static readonly string[] CheckedExtensions = [".cs", ".csproj", ".props", ".targets", ".sln", ".slnx"];

    [Fact]
    public void Code_HasNoObservabilityNamespaceOrPackageReference()
    {
        var root = RepositoryRoot();
        var offenders = CheckedDirectories
            .Select(directory => Path.Combine(root, directory))
            .Where(Directory.Exists)
            .Append(root)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*",
                directory == root ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
            .Where(file => CheckedExtensions.Contains(Path.GetExtension(file)))
            .Where(file => !IsBuildOutput(file))
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => (file, line, number: index + 1))
                .Where(entry => RemovedReference().IsMatch(entry.line)))
            .Select(entry => $"{Path.GetRelativePath(root, entry.file)}:{entry.number}: {entry.line.Trim()}")
            .ToList();

        offenders.Should().BeEmpty("telemetry lives in Anarchy.Toolkit.Core.Telemetry and Anarchy.Toolkit.Infrastructure.Telemetry.Serilog");
    }

    private static bool IsBuildOutput(string file) =>
        file.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj");

    [GeneratedRegex(@"Anarchy\.Toolkit\.[\w.]*Observ" + "ability|\\b(namespace|using)\\s+[\\w.]*\\.Observ" + "ability\\b|Observ" + "ability[\\w.]*\\.csproj")]
    private static partial Regex RemovedReference();

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", ".."));
}
