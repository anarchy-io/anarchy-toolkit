using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.CodeGeneration;
using Fallout.Common;
using Fallout.Common.IO;
using System.Text.RegularExpressions;

namespace Build;

internal sealed partial class Build
{
    private AbsolutePath FalloutProjectDirectory => this.As<IDefaultBuild>().SourceDirectory / "Build.Fallout";

    private AbsolutePath ToolWrapperDirectory => FalloutProjectDirectory / "Infrastructure" / "Service" / "Tool";

    /// <summary>
    /// Regenerates the CLI wrappers of <c>Anarchy.Toolkit.Build.Fallout</c> from its tool specifications, one type per
    /// file under a folder per tool (matching its namespace)
    /// </summary>
    public Target GenerateTools => d => d
        .Description("Regenerates Build.Fallout's CLI tool wrappers from specification/*.json")
        .Executes(() => {
            ToolWrapperDirectory.GlobFiles("**/*.Generated.cs").DeleteFiles();
            CodeGenerator.GenerateCodeFromDirectory(
                specificationDirectory: FalloutProjectDirectory / "specification",
                outputFileProvider: tool => ToolWrapperDirectory / tool.DefaultOutputFileName,
                namespaceProvider: tool => $"Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.{tool.Name}");
            foreach (var file in ToolWrapperDirectory.GlobFiles("*.Generated.cs"))
                SplitGeneratedTypes(file, ToolWrapperDirectory / file.Name.Replace(".Generated.cs", ""));
        });

    /// <summary>
    /// Splits a generated file into one file per top-level type, each named after its type. The generator writes
    /// types at column 0, closed by a <c>}</c> at column 0 and optionally wrapped in a <c>#region</c>.
    /// </summary>
    private static void SplitGeneratedTypes(AbsolutePath file, AbsolutePath directory)
    {
        var lines = File.ReadAllLines(file);
        var namespaceLine = Array.FindIndex(lines, line => line.StartsWith("namespace ", StringComparison.Ordinal));
        if (namespaceLine < 0) throw new InvalidOperationException($"{file} declares no namespace.");
        var header = string.Join("\n", lines[..(namespaceLine + 1)]);

        var types = new List<(string Name, List<string> Lines)>();
        List<string>? current = null;
        string? name = null;
        for (var index = namespaceLine + 1; index < lines.Length; index++)
        {
            var line = lines[index];
            if (current is null)
            {
                if (line.Trim().Length == 0) continue;
                current = [];
            }
            current.Add(line);
            name ??= Regex.Match(line, @"^(?:public|internal)\b[^(]*?\b(?:class|interface|enum|struct|record)\s+(\w+)") is { Success: true } match
                ? match.Groups[1].Value
                : null;
            if (line != "}") continue;

            if (index + 1 < lines.Length && lines[index + 1].StartsWith("#endregion", StringComparison.Ordinal))
                current.Add(lines[++index]);
            types.Add((name ?? throw new InvalidOperationException($"No type declaration before line {index + 1} of {file}."), current));
            current = null;
            name = null;
        }
        if (current is not null && current.Any(line => line.Trim().Length > 0))
            throw new InvalidOperationException($"{file} ends with an incomplete type.");

        directory.CreateDirectory();
        foreach (var (typeName, typeLines) in types)
            File.WriteAllText(directory / $"{typeName}.Generated.cs", header + "\n\n" + string.Join("\n", typeLines) + "\n");
        file.DeleteFile();
    }
}
