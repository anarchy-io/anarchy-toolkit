using Anarchy.Toolkit.Core.Health.Entity.Attribute;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Entity.Service.Health;
using System.Reflection;
using System.Runtime.Versioning;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health
{
    /// <summary>
    /// Reads build metadata from an assembly-embedded resource in JSON format.
    /// </summary>
    public class BuildMetadataFromEmbeddedResource : IBuildMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="BuildMetadataFromEmbeddedResource"/> with the specified assembly.
        /// </summary>
        public BuildMetadataFromEmbeddedResource(Assembly assembly)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            ApplicationName = assembly.GetName().Name!;
            ApplicationVersion = assembly.GetName().Version!.ToString();
            TargetFramework = GetTargetFramework(assembly);

            var metadata = ReadMetadata(assembly);

            BuildCommitHash = metadata.GitSha1;
            BuildMachineName = metadata.BuildMachine;
            BuildNumber = metadata.BuildNumber;
            BuildTimestamp = metadata.BuiltWhen;
            BuildUsername = metadata.BuiltBy;

            RunbookUri = GetRunbookUri(assembly);
        }

        private static string? GetTargetFramework(Assembly assembly)
        {
            var attribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
            return attribute?.FrameworkName;
        }

        /// <inheritdoc />
        public string ApplicationName { get; }

        /// <inheritdoc />
        public string ApplicationVersion { get; }

        /// <inheritdoc />
        public string? TargetFramework { get; }

        /// <inheritdoc />
        public string? BuildNumber { get; }

        /// <inheritdoc />
        public string? BuildMachineName { get; }

        /// <inheritdoc />
        public string? BuildUsername { get; }

        /// <inheritdoc />
        public DateTimeOffset? BuildTimestamp { get; }

        /// <inheritdoc />
        public string? BuildCommitHash { get; }

        /// <inheritdoc />
        public string? RunbookUri { get; }

        private BuildMetadata ReadMetadata(Assembly assembly)
        {
            var reader = new BuildMetadataReader();
            return reader.ReadMetadata(assembly);
        }

        private string? GetRunbookUri(Assembly assembly)
        {
            var attribute = assembly.GetCustomAttribute<ApplicationRunbookUriAttribute>();

            return attribute?.Uri;
        }
    }
}
