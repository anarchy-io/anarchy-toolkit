using Anarchy.Toolkit.Hosting.Workers.Entity.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Helper.Resource;
using System.Reflection;
using System.Text.Json;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health
{
    internal class BuildMetadataReader
    {
        private const string _metadataFileName = "build.meta.json";
        private const string _metadataDirectoryName = "Properties";
        private const string _metadataNotFoundMessage = _metadataFileName + " not found";

        private static readonly BuildMetadata _emptyBuildMetadata =
            new() {
                BuildMachine = _metadataNotFoundMessage,
                BuildNumber = _metadataNotFoundMessage,
                BuiltBy = _metadataNotFoundMessage,
                GitSha1 = _metadataNotFoundMessage
            };

        public BuildMetadata ReadMetadata(Assembly assembly)
        {
            var reader = new TextResourceReader(assembly, _metadataDirectoryName);
            var json = reader.Read(_metadataFileName);

            return json is null
                       ? _emptyBuildMetadata
                       : JsonSerializer.Deserialize<BuildMetadata>(json)!;
        }
    }
}
