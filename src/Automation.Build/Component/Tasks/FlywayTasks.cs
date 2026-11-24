using Anarchy.Toolkit.Automation.Core.Service.Container;

namespace Anarchy.Toolkit.Automation.Build.Component.Tasks;

internal static class FlywayTasks
{
    internal static void Run(IContainerCli cli, string root, string image, string? network,
        IReadOnlyList<string> configFiles, IReadOnlyList<string> locations, string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);
        if (image.StartsWith('-')) throw new ArgumentException("Expected a Docker image name, not an option.", nameof(image));
        if (configFiles.Count == 0) throw new InvalidOperationException("Set FlywayConfigFiles to at least one Flyway configuration file.");

        var arguments = new List<string> { "run", "--rm" };
        if (!string.IsNullOrWhiteSpace(network)) arguments.AddRange(["--network", network]);

        var containerConfigFiles = new List<string>();
        foreach (var file in configFiles)
        {
            var source = Path.GetFullPath(file, root);
            if (!File.Exists(source)) throw new InvalidOperationException($"Flyway configuration file not found: {source}");

            // Flyway uses the file extension to distinguish TOML from classic .conf configuration.
            var destination = $"/flyway/anarchy/config/{containerConfigFiles.Count}{Path.GetExtension(source)}";
            AddMount(arguments, source, destination);
            containerConfigFiles.Add(destination);
        }

        var containerLocations = new List<string>();
        foreach (var directory in locations)
        {
            var source = Path.GetFullPath(directory, root);
            if (!Directory.Exists(source)) throw new InvalidOperationException($"Flyway migration directory not found: {source}");

            var destination = $"/flyway/anarchy/sql/{containerLocations.Count}";
            AddMount(arguments, source, destination);
            containerLocations.Add($"filesystem:{destination}");
        }

        arguments.AddRange([image, $"-configFiles={string.Join(',', containerConfigFiles)}"]);
        if (containerLocations.Count != 0) arguments.Add($"-locations={string.Join(',', containerLocations)}");
        arguments.Add(command);

        var exitCode = cli.Stream("docker", arguments);
        if (exitCode != 0) throw new InvalidOperationException($"Flyway {command} failed with exit code {exitCode}.");
    }

    private static void AddMount(List<string> arguments, string source, string destination) => arguments.AddRange([
        "--mount", $"type=bind,{MountField($"source={source}")},{MountField($"target={destination}")},readonly"
    ]);

    // Docker parses the --mount argument as CSV; quote the entire field, not just its value.
    private static string MountField(string field) => field.IndexOfAny([',', '"', '\r', '\n']) < 0
        ? field
        : $"\"{field.Replace("\"", "\"\"")}\"";
}
