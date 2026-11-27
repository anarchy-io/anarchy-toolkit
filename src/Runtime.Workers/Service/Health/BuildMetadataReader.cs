using Anarchy.Toolkit.Runtime.Workers.Entity.Service.Health;
using Anarchy.Toolkit.Runtime.Workers.Helpers.Resources;
using System.Reflection;
using System.Text.Json;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Health;

internal class BuildMetadataReader
{
    private const string MetadataFileName = "build.meta.json";
    private const string MetadataDirectoryName = "Properties";
    private const string MetadataNotFoundMessage = MetadataFileName + " not found";

    private static readonly BuildMetadata EmptyBuildMetadata =
        new() {
            BuildMachine = MetadataNotFoundMessage,
            BuildNumber = MetadataNotFoundMessage,
            BuiltBy = MetadataNotFoundMessage,
            GitSha1 = MetadataNotFoundMessage
        };

    public static BuildMetadata ReadMetadata(Assembly assembly)
    {
        var reader = new TextResourceReader(assembly, MetadataDirectoryName);
        var json = reader.Read(MetadataFileName);

        return json is null
            ? EmptyBuildMetadata
            : JsonSerializer.Deserialize<BuildMetadata>(json)!;
    }
}
