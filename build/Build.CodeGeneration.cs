using Anarchy.Toolkit.Build.Fallout.Component;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
using Fallout.CodeGeneration;
using Fallout.Common;
using Fallout.Common.IO;

namespace Build;

internal sealed partial class Build
{
    private AbsolutePath FalloutProjectDirectory => this.As<IDefaultBuild>().SourceDirectory / "Build.Fallout";

    /// <summary>
    /// Regenerates the CLI wrappers of <c>Anarchy.Toolkit.Build.Fallout</c> from its tool specifications
    /// </summary>
    public Target GenerateTools => d => d
        .Description("Regenerates Build.Fallout's CLI tool wrappers from specification/*.json")
        .Executes(() => CodeGenerator.GenerateCodeFromDirectory(
            specificationDirectory: FalloutProjectDirectory / "specification",
            outputFileProvider: tool => FalloutProjectDirectory / "Infrastructure" / "Service" / "Tool" / tool.DefaultOutputFileName,
            namespaceProvider: tool => $"Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.{tool.Name}"));
}
