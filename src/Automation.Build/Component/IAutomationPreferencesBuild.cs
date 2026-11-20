using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Service;
using Fallout.Common;
using Fallout.Common.IO;
using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>Optional solution preferences and explicit editing targets, independent of solution loading.</summary>
public interface IAutomationPreferencesBuild : IFalloutBuild
{
    /// <summary>Saved overrides; reading never writes the preferences file.</summary>
    AutomationPreferences Preferences => AutomationPreferences.Read(RootDirectory);
    /// <summary>Reads a typed override; nullable value types retain absence.</summary>
    T? Preference<T>(string key) => Preferences.Get<T>(key);
    /// <summary>Resolves an optional saved path relative to the solution root.</summary>
    AbsolutePath? PreferencePath(string key) => Preference<string>(key) is { } path ? AbsolutePath.Create(Path.GetFullPath(path, RootDirectory)) : null;

    /// <summary>Ordered skill directories; installation and synchronization create skills in the first.</summary>
    [Parameter("Agent skill directories, relative to the root (default: .agents/skills)")]
    string[] AgentSkillsDirectories => TryGetValue(() => AgentSkillsDirectories) ?? Preference<string[]>("agentSkills.directories") ?? [AgentSkillTasks.DefaultDirectory];

    /// <summary>Explicit layering mode; folder mode is the in-code default.</summary>
    [Parameter("Layering mode: folder or prefix", Name = "LayeringMode")]
    string? LayeringModeOverride => TryGetValue(() => LayeringModeOverride);
    /// <summary>Explicit ordered layer names; an empty collection replaces saved values and is invalid.</summary>
    [Parameter("Layer names, innermost first")]
    string[]? LayeringLayers => TryGetValue(() => LayeringLayers);
    /// <summary>Explicit checked-project root.</summary>
    [Parameter("Layering checked root (default: src)")]
    string? LayeringRoot => TryGetValue(() => LayeringRoot);
    /// <summary>Explicit folder-mode library root.</summary>
    [Parameter("Folder-mode library root (default: src/Library)")]
    string? LayeringLibraryRoot => TryGetValue(() => LayeringLibraryRoot);
    /// <summary>Explicit folder-to-layer mappings as a JSON object; an empty object clears saved mappings.</summary>
    [Parameter("Application folder mappings as JSON")]
    string? LayeringApplicationFolders => TryGetValue(() => LayeringApplicationFolders);

    /// <summary>Resolves all invocation layering overrides over saved preferences and the in-code defaults.</summary>
    SolutionLayout ResolveLayering()
    {
        var overrides = new JsonObject();
        if (LayeringModeOverride is { } mode) overrides["mode"] = mode;
        if (LayeringLayers is { } layers) overrides["layers"] = JsonSerializer.SerializeToNode(layers);
        if (LayeringRoot is { } root) overrides["root"] = root;
        if (LayeringLibraryRoot is { } library) overrides["libraryRoot"] = library;
        if (LayeringApplicationFolders is { } folders) overrides["applicationFolders"] = JsonNode.Parse(folders);
        return Preferences.ResolveLayout(overrides);
    }

    /// <summary>The section/property to edit.</summary>
    [Parameter("Preference path, for example docker.timeout")]
    string? PreferenceKey => TryGetValue(() => PreferenceKey);
    /// <summary>The typed JSON value to save.</summary>
    [Parameter("JSON preference value, for example 90, false or [\"postgres\"]")]
    string? PreferenceValue => TryGetValue(() => PreferenceValue);

    /// <summary>Shows saved overrides and fallback descriptions without writing.</summary>
    Target ShowAnarchyPreferences => d => d.Description("Shows saved Anarchy overrides and fallback descriptions")
        .Executes(() => {
            var invocation = new Dictionary<string, (JsonNode? Value, string Source)>();
            if (LayeringModeOverride is { } mode) invocation["layering.mode"] = (JsonValue.Create(mode), "Fallout parameter");
            if (LayeringLayers is { } layers) invocation["layering.layers"] = (JsonSerializer.SerializeToNode(layers), "Fallout parameter");
            if (LayeringRoot is { } root) invocation["layering.root"] = (JsonValue.Create(root), "Fallout parameter");
            if (LayeringLibraryRoot is { } library) invocation["layering.libraryRoot"] = (JsonValue.Create(library), "Fallout parameter");
            if (LayeringApplicationFolders is { } folders)
                invocation["layering.applicationFolders"] = (JsonNode.Parse(folders), "Fallout parameter");
            var description = Preferences.Describe(invocation, ResolveLayering());
            if (Verbosity != Fallout.Common.Verbosity.Quiet) Console.WriteLine(description);
        });
    /// <summary>Explicitly saves a typed preference, retaining unrelated overrides.</summary>
    Target SetAnarchyPreference => d => d.Description("Saves one typed Anarchy preference")
        .Requires(() => PreferenceKey).Requires(() => PreferenceValue)
        .Executes(() => Preferences.With(PreferenceKey!, PreferenceValue!).Save(RootDirectory));
    /// <summary>Removes one override, restoring fallback behavior.</summary>
    Target UnsetAnarchyPreference => d => d.Description("Removes an Anarchy override")
        .Requires(() => PreferenceKey)
        .Executes(() => Preferences.With(PreferenceKey!, null).Save(RootDirectory));
    /// <summary>Previews an interactive preference edit and asks before saving.</summary>
    Target ConfigureAnarchyPreferences => d => d.Description("Interactively edits an optional Anarchy override")
        .Executes(() => {
            if (IsServerBuild || Console.IsInputRedirected)
                throw new InvalidOperationException("Interactive input unavailable; use SetAnarchyPreference or UnsetAnarchyPreference.");
            var key = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Preference").AddChoices(AutomationPreferences.Keys));
            var value = AnsiConsole.Ask<string>("JSON value (use config unset to restore a default):");
            var edited = Preferences.With(key, value);
            Console.WriteLine(edited.Describe());
            if (AnsiConsole.Confirm("Save these overrides?", false)) edited.Save(RootDirectory);
        });
}
