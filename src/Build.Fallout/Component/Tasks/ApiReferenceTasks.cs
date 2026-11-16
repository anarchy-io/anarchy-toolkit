using Docfx;
using Docfx.Common;
using Docfx.Dotnet;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using System.Reflection;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks;

/// <summary>
/// A project to document: its path, and the target framework to document when it targets several.
/// </summary>
public sealed record ApiReferenceProject(string Path, string? TargetFramework);

/// <summary>
/// Tasks generating a DocFX API reference from compiled assemblies, so DocFX does not compile the projects again.
/// </summary>
public static class ApiReferenceTasks
{
    private const string CollectTarget = "AnarchyToolkitCollectApiReference";

    /// <summary>
    /// Resolves the assembly of each project and the references it was compiled against, without building:
    /// a single MSBuild run of <c>ResolveReferences</c> over every project, in parallel. The projects must be built.
    /// </summary>
    /// <returns>The assemblies to document and every reference needed to load them.</returns>
    public static (IReadOnlyList<AbsolutePath> Assemblies, IReadOnlyList<AbsolutePath> References) ResolveAssemblies(
        IReadOnlyCollection<ApiReferenceProject> projects, string configuration, AbsolutePath workingDirectory)
    {
        workingDirectory.CreateOrCleanDirectory();
        var targets = workingDirectory / "ApiReference.targets";
        using (var resource = typeof(ApiReferenceTasks).Assembly.GetManifestResourceStream("ApiReference.targets")!)
        using (var file = File.Create(targets))
            resource.CopyTo(file);

        var manifests = projects.Select((project, index) => (Project: project, Manifest: workingDirectory / $"{index}.txt")).ToList();
        var traversal = workingDirectory / "ApiReference.proj";
        traversal.WriteAllText(RenderTraversal(manifests, targets, configuration));

        DotNetTasks.DotNetMSBuild(s => s
            .SetTargetPath(traversal)
            .SetMaxCpuCount(null)
            .SetVerbosity(DotNetVerbosity.quiet)
            .SetNodeReuse(false));

        var assemblies = new List<AbsolutePath>();
        var references = new HashSet<AbsolutePath>();
        foreach (var (_, manifest) in manifests)
        {
            var lines = File.ReadAllLines(manifest).Where(line => line.Length > 0).ToArray();
            assemblies.Add(AbsolutePath.Create(lines[0]));
            references.UnionWith(lines.Skip(1).Select(AbsolutePath.Create));
        }

        references.ExceptWith(assemblies);

        // Projects may resolve different versions of a package: DocFX takes one file per assembly name.
        var latestReferences = references
            .GroupBy(reference => reference.NameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MaxBy(reference => AssemblyName.GetAssemblyName(reference).Version)!)
            .ToList();
        return (assemblies, latestReferences);
    }

    /// <summary>
    /// Generates the API reference YAML of the assemblies into <paramref name="metadataOutput"/>, then builds the
    /// DocFX site configured by the <c>build</c> section of <paramref name="docfxConfig"/>. Its <c>metadata</c>
    /// section, if any, is replaced by the assemblies.
    /// </summary>
    public static async Task Generate(
        AbsolutePath docfxConfig,
        AbsolutePath metadataOutput,
        IReadOnlyCollection<AbsolutePath> assemblies,
        IReadOnlyCollection<AbsolutePath> references)
    {
        var config = JsonNode.Parse(docfxConfig.ReadAllText())!.AsObject();
        config["metadata"] = new JsonArray(new JsonObject {
            ["src"] = ToFileMappings(assemblies),
            ["references"] = ToFileMappings(references),
            ["output"] = metadataOutput.ToString(),
            ["namespaceLayout"] = "nested",
            ["enumSortOrder"] = "declaringOrder",
        });

        // Next to the original, so the relative paths of its build section still resolve.
        var generatedConfig = docfxConfig.Parent / $".{docfxConfig.NameWithoutExtension}.generated.json";
        generatedConfig.WriteAllText(config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        try
        {
            metadataOutput.CreateOrCleanDirectory();
            Logger.ResetCount();
            await DotnetApiCatalog.GenerateManagedReferenceYamlFiles(generatedConfig);
            await Docset.Build(generatedConfig);
            Assert.False(Logger.HasError, $"DocFX reported {Logger.ErrorCount} errors");
        }
        finally
        {
            generatedConfig.DeleteFile();
        }
    }

    private static JsonArray ToFileMappings(IEnumerable<AbsolutePath> files) =>
        new(files
            .GroupBy(file => file.Parent.ToString())
            .Select(group => (JsonNode)new JsonObject {
                ["src"] = group.Key,
                ["files"] = new JsonArray(group.Select(file => (JsonNode)file.Name).ToArray()),
            })
            .ToArray());

    private static string RenderTraversal(
        IEnumerable<(ApiReferenceProject Project, AbsolutePath Manifest)> manifests, AbsolutePath targets, string configuration)
    {
        var items = manifests.Select(entry => {
            var properties = $"AnarchyToolkitApiReferenceManifest={entry.Manifest}";
            if (entry.Project.TargetFramework is { } targetFramework) properties += $";TargetFramework={targetFramework}";
            return $"""    <ApiReferenceProject Include="{SecurityElement.Escape(entry.Project.Path)}" AdditionalProperties="{SecurityElement.Escape(properties)}" />""";
        });

        return $"""
            <Project>
              <ItemGroup>
            {string.Join("\n", items)}
              </ItemGroup>
              <Target Name="Build">
                <MSBuild Projects="@(ApiReferenceProject)" Targets="{CollectTarget}" BuildInParallel="true"
                         Properties="Configuration={configuration};BuildProjectReferences=false;CustomAfterMicrosoftCommonTargets={SecurityElement.Escape(targets)}" />
              </Target>
            </Project>
            """;
    }
}
