using Anarchy.Toolkit.Hosting.Workers.Entity.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Helper.Resource;
using System.Reflection;
using System.Text.Json;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health;

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
