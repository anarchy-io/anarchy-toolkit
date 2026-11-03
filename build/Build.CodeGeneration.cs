using Anarchy.Toolkit.Build.Nuke.Component;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Extension;
using Nuke.CodeGeneration;
using Nuke.Common;
using Nuke.Common.IO;

namespace Build;

internal sealed partial class Build
{
    private AbsolutePath NukeProjectDirectory => this.As<IDefaultBuild>().SourceDirectory / "Build.Nuke";

    /// <summary>
    /// Regenerates the CLI wrappers of <c>Anarchy.Toolkit.Build.Nuke</c> from its tool specifications
    /// </summary>
    public Target GenerateTools => d => d
        .Description("Regenerates Build.Nuke's CLI tool wrappers from specification/*.json")
        .Executes(() => CodeGenerator.GenerateCodeFromDirectory(
            specificationDirectory: NukeProjectDirectory / "specification",
            outputFileProvider: tool => NukeProjectDirectory / "Infrastructure" / "Service" / "Tool" / tool.DefaultOutputFileName,
            namespaceProvider: tool => $"Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.{tool.Name}"));
}
