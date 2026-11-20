using Anarchy.Toolkit.Automation.Core.Entity;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>Reads and explicitly edits optional solution automation overrides. Reading never creates a file.</summary>
public sealed class AutomationPreferences
{
    /// <summary>The optional file at the solution root.</summary>
    public const string FileName = ".anarchy-toolkit.json";
    /// <summary>The schema shared by the editor and runtime validation.</summary>
    public const string SchemaUrl = "https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json";
    private static readonly JsonDocumentOptions DocumentOptions = new() {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        AllowDuplicateProperties = false
    };
    private static readonly JsonSerializerOptions OutputOptions = new() { WriteIndented = true };
    private static readonly Lazy<JsonObject> Schema = new(() => {
        using var stream = typeof(AutomationPreferences).Assembly.GetManifestResourceStream("Anarchy.Automation.Schema")!;
        return JsonNode.Parse(stream)!.AsObject();
    });
    private readonly JsonObject _document;

    private AutomationPreferences(JsonObject document) => _document = document;

    /// <summary>Reads overrides, or an empty document when no preferences have been saved.</summary>
    public static AutomationPreferences Read(string root) => File.Exists(Path.Combine(root, FileName))
        ? Parse(File.ReadAllText(Path.Combine(root, FileName)), Path.Combine(root, FileName))
        : Parse("{}");

    /// <summary>Parses and validates only supplied overrides, without inserting defaults.</summary>
    public static AutomationPreferences Parse(string json, string source = FileName)
    {
        try
        {
            var document = JsonNode.Parse(json, documentOptions: DocumentOptions) as JsonObject
                           ?? throw new InvalidOperationException($"{source}: expected an object.");
            ValidateNode(document, Schema.Value, source);
            var preferences = new AutomationPreferences(document);
            preferences.ResolveLayout();
            foreach (var directory in preferences.Get<string[]>("agentSkills.directories") ?? [])
                AgentSkillTasks.NormalizeDirectory(directory);
            return preferences;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"{source}: invalid JSON: {exception.Message}", exception);
        }
        catch (InvalidOperationException exception) when (!exception.Message.StartsWith(source, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{source}: {exception.Message}", exception);
        }
    }

    /// <summary>Gets a typed override; use nullable value types to distinguish omission from false or zero.</summary>
    public T? Get<T>(string key) => Find(_document, key) is { } node ? node.Deserialize<T>() : default;

    /// <summary>A copy of saved values only, never populated with effective defaults.</summary>
    public JsonObject Saved => (JsonObject)_document.DeepClone();

    /// <summary>Resolves an explicit layering patch over saved values and the toolkit's folder-mode defaults.</summary>
    public SolutionLayout ResolveLayout(JsonObject? overrides = null)
    {
        var values = MergeLayering(_document["layering"] as JsonObject, overrides);
        ValidateNode(values, Schema.Value["properties"]!["layering"]!.AsObject(), "layering");
        var mode = values["mode"]?.GetValue<string>() == "prefix" ? LayeringMode.Prefix : LayeringMode.Folder;
        if (mode == LayeringMode.Prefix && (values.ContainsKey("libraryRoot") || values.ContainsKey("applicationFolders")))
            throw new InvalidOperationException($"{FileName}: layering: prefix mode cannot have libraryRoot or applicationFolders; a solution uses one layering mode.");
        var layers = values["layers"]?.Deserialize<string[]>() ?? SolutionLayout.DefaultLayers;
        var root = values["root"]?.GetValue<string>() ?? SolutionLayout.DefaultRoot;
        var baseline = new SolutionLayout(mode, layers) { Root = root };
        var layout = baseline with {
            LibraryRoot = values["libraryRoot"]?.GetValue<string>() ?? (mode == LayeringMode.Folder ? $"{root}/Library" : null),
            ApplicationFolders = values["applicationFolders"]?.Deserialize<Dictionary<string, string>>()
                                 ?? (mode == LayeringMode.Folder
                                     ? SolutionLayout.Kinds.Where(kind => layers.Contains(SolutionLayout.LayerOf(kind)))
                                         .ToDictionary(kind => $"{root}/{SolutionLayout.FolderOf(kind)}", SolutionLayout.LayerOf)
                                     : new Dictionary<string, string>())
        };
        if (LayeringConfiguration.Validate(layout) is { Count: > 0 } errors)
            throw new InvalidOperationException($"{FileName}: layering: {string.Join("; ", errors)}");
        return layout;
    }

    /// <summary>Creates a sparse layering patch suitable for explicit persistence or invocation arguments.</summary>
    public static JsonObject LayoutOverrides(SolutionLayout layout)
    {
        var values = new JsonObject {
            ["mode"] = layout.Mode == LayeringMode.Folder ? "folder" : "prefix",
            ["layers"] = JsonSerializer.SerializeToNode(layout.Layers),
            ["root"] = layout.Root
        };
        if (layout.Mode == LayeringMode.Folder)
        {
            values["libraryRoot"] = layout.LibraryRoot;
            values["applicationFolders"] = JsonSerializer.SerializeToNode(layout.ApplicationFolders);
        }
        return values;
    }

    /// <summary>Previews a typed edit without changing any files. Null removes an override.</summary>
    public AutomationPreferences With(string key, string? json) => WithEdits(new Dictionary<string, string?> { [key] = json });

    /// <summary>Previews a batch of targeted edits, validating their final combination rather than intermediate states.</summary>
    public AutomationPreferences WithEdits(IReadOnlyDictionary<string, string?> edits)
    {
        var properties = Schema.Value["properties"]!.AsObject();
        var edited = Saved;
        foreach (var key in edits.Keys)
        {
            var parts = key.Split('.');
            if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Use a preference path such as docker.timeout.", nameof(edits));
            if (!properties.ContainsKey(parts[0]) || properties[parts[0]]?["properties"]?.AsObject().ContainsKey(parts[1]) != true)
                throw new ArgumentException($"Unknown preference '{key}'.", nameof(edits));
        }
        // Clear incompatible mode-specific fields before applying the rest of the same transaction.
        if (edits.TryGetValue("layering.mode", out var mode) && mode is not null)
            edited["layering"] = MergeLayering(edited["layering"] as JsonObject,
                new JsonObject { ["mode"] = JsonNode.Parse(mode, documentOptions: DocumentOptions) });
        foreach (var (key, json) in edits)
        {
            var parts = key.Split('.');
            if (edited[parts[0]] is null) edited[parts[0]] = new JsonObject();
            var parent = edited[parts[0]]!.AsObject();
            if (json is null) parent.Remove(parts[1]);
            else parent[parts[1]] = JsonNode.Parse(json, documentOptions: DocumentOptions);
            if (parent.Count == 0) edited.Remove(parts[0]);
        }
        return Parse(edited.ToJsonString());
    }

    /// <summary>Merges deliberately selected layout overrides, clearing incompatible old-mode fields on a mode switch.</summary>
    public AutomationPreferences WithLayering(JsonObject overrides)
    {
        var edited = Saved;
        edited["layering"] = MergeLayering(edited["layering"] as JsonObject, overrides);
        return Parse(edited.ToJsonString());
    }

    private static JsonObject MergeLayering(JsonObject? saved, JsonObject? overrides)
    {
        var values = saved?.DeepClone().AsObject() ?? new JsonObject();
        if (overrides is null) return values;
        // A mode change owns its mode-specific fields, not unrelated source roots or layer names.
        if (overrides["mode"] is not null && overrides["mode"]!.ToJsonString() != (values["mode"]?.ToJsonString() ?? "\"folder\""))
        {
            values.Remove("libraryRoot");
            values.Remove("applicationFolders");
        }
        foreach (var (key, value) in overrides) values[key] = value?.DeepClone();
        return values;
    }

    /// <summary>Explicitly saves validated overrides atomically, retaining unrelated values. Never called by reads.</summary>
    public void Save(string root)
    {
        var document = Saved;
        var path = Path.Combine(Path.GetFullPath(root), FileName);
        if (document.Count == 0 && !File.Exists(path)) return;
        document["$schema"] = SchemaUrl;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, document.ToJsonString(OutputOptions) + "\n");
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>Shows saved overrides, effective sources and fallback descriptions without writing a file.</summary>
    public string Describe() => Describe(null, null);

    /// <summary>Shows preferences along with the effective values supplied by the calling build invocation.</summary>
    /// <param name="invocationValues">Effective per-key values supplied by the calling build invocation.</param>
    /// <param name="effectiveLayout">The layout resolved by the caller, including invocation parameters.</param>
    public string Describe(
        IReadOnlyDictionary<string, (JsonNode? Value, string Source)>? invocationValues,
        SolutionLayout? effectiveLayout) => new JsonObject {
            ["saved"] = Saved,
            ["fallbacks"] = Schema.Value["properties"]!.DeepClone(),
            ["effectiveLayering"] = LayoutOverrides(effectiveLayout ?? ResolveLayout()),
            ["effective"] = DescribeEffectiveValues(invocationValues)
        }.ToJsonString(OutputOptions);

    /// <summary>Every editable section/property path, shared by both interactive front ends.</summary>
    public static IReadOnlyList<string> Keys => Schema.Value["properties"]!.AsObject()
        .Where(pair => pair.Key != "$schema")
        .SelectMany(section => section.Value!["properties"]!.AsObject().Select(property => section.Key + "." + property.Key)).ToArray();

    private static JsonNode? Find(JsonObject document, string key) => key.Split('.').Aggregate((JsonNode?)document, (node, part) => node?[part]);

    private JsonObject DescribeEffectiveValues(IReadOnlyDictionary<string, (JsonNode? Value, string Source)>? invocationValues)
    {
        var result = new JsonObject();
        foreach (var (section, sectionSchema) in Schema.Value["properties"]!.AsObject())
        {
            if (section == "$schema") continue;
            foreach (var (property, propertySchema) in sectionSchema!["properties"]!.AsObject())
            {
                var key = $"{section}.{property}";
                var saved = Find(_document, key);
                var fallback = propertySchema!["default"];
                if (invocationValues?.TryGetValue(key, out var invocation) == true)
                {
                    result[key] = new JsonObject {
                        ["source"] = invocation.Source,
                        ["value"] = invocation.Value?.DeepClone()
                    };
                    continue;
                }
                var effective = new JsonObject {
                    ["source"] = saved is not null ? "saved value" : "in-code default"
                };
                if (saved is not null) effective["value"] = saved.DeepClone();
                else if (fallback is not null) effective["value"] = fallback.DeepClone();
                else if (propertySchema["description"] is JsonValue description)
                    effective["fallback"] = description.GetValue<string>();
                result[key] = effective;
            }
        }
        return result;
    }

    private static void ValidateNode(JsonNode? node, JsonObject schema, string path)
    {
        var type = schema["type"]!.GetValue<string>();
        var valid = type switch {
            "object" => node is JsonObject,
            "array" => node is JsonArray,
            "string" => node is JsonValue text && text.TryGetValue<string>(out _),
            "boolean" => node is JsonValue boolean && boolean.TryGetValue<bool>(out _),
            "integer" => node is JsonValue integer && integer.TryGetValue<int>(out _),
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"{path}: expected {type}.");
        if (schema["enum"] is JsonArray choices && !choices.Any(choice => JsonNode.DeepEquals(choice, node)))
            throw new InvalidOperationException($"{path}: expected one of {choices.ToJsonString()}.");
        if (node is JsonValue value)
        {
            if (type == "string" && schema["minLength"] is not null && value.GetValue<string>().Length < schema["minLength"]!.GetValue<int>())
                throw new InvalidOperationException($"{path}: must not be empty.");
            if (type == "integer" && ((schema["minimum"] is not null && value.GetValue<int>() < schema["minimum"]!.GetValue<int>()) ||
                                      (schema["maximum"] is not null && value.GetValue<int>() > schema["maximum"]!.GetValue<int>())))
                throw new InvalidOperationException($"{path}: outside the allowed range.");
        }
        if (node is JsonArray array)
            foreach (var item in array) ValidateNode(item, schema["items"]!.AsObject(), path + "[]");
        if (node is not JsonObject obj) return;
        foreach (var (key, child) in obj)
        {
            var childSchema = schema["properties"]?[key] as JsonObject ?? schema["additionalProperties"] as JsonObject;
            if (childSchema is null) throw new InvalidOperationException($"{path}.{key}: unknown property.");
            ValidateNode(child, childSchema, path + "." + key);
        }
    }
}
