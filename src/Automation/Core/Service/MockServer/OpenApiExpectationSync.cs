using Anarchy.Toolkit.Automation.Core.Entity;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Automation.Core.Service.MockServer;

/// <summary>
/// Adds mock server expectations (the <c>Anarchy.Toolkit.Web.MockServer</c> JSON format) for the endpoints of an
/// OpenAPI specification that have none yet, for a developer to review, adjust and commit.
/// </summary>
/// <remarks>
/// It only ever adds: existing expectations and their formatting are never changed. Expectations whose endpoint is
/// gone from the specification, changed, or answers with an undocumented status code are reported as
/// <see cref="OpenApiSyncNotice"/>s and left alone. An endpoint counts as covered when any expectation has its method
/// and path (with any parameter names) or its operation id. New expectations are appended to <c>&lt;tag&gt;.json</c> in
/// the output directory, after the file's existing ones; endpoints without a tag go to <c>default.json</c>.
/// </remarks>
public static partial class OpenApiExpectationSync
{
    private const int MaxSchemaDepth = 6;

    private static readonly JsonSerializerOptions Indented = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonDocumentOptions Lenient = new() {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Reads <paramref name="specification"/> and adds an expectation to <paramref name="outputDirectory"/> for each of
    /// its endpoints that has none, reporting differences it leaves to the developer.
    /// </summary>
    /// <param name="specification">A local OpenAPI or Swagger file (JSON or YAML), or an <c>http(s)</c> URL.</param>
    /// <param name="outputDirectory">The expectations directory; every <c>*.json</c> below it is considered existing.</param>
    /// <param name="dryRun">Reports what would be added without writing anything.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>What was added and what the developer should look at.</returns>
    /// <exception cref="InvalidOperationException">The specification could not be read.</exception>
    public static async Task<OpenApiSyncReport> SyncAsync(
        string specification, string outputDirectory, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var document = await LoadAsync(specification, warnings, cancellationToken);
        var operations = Operations(document).ToList();
        var existing = ReadExisting(outputDirectory, warnings);

        var notices = new List<OpenApiSyncNotice>();
        var added = new List<(OpenApiSyncAddition Addition, JsonObject Expectation)>();

        foreach (var operation in operations)
        {
            var byEndpoint = existing.Where(e => e.Matches(operation)).ToList();
            var byId = existing.Where(e => e.Id == operation.Id && !e.Matches(operation)).ToList();

            foreach (var expectation in byId)
            {
                notices.Add(new OpenApiSyncNotice(OpenApiSyncNoticeKind.EndpointChanged, expectation.Name, expectation.File,
                    $"'{expectation.Name}' is {expectation.Method} {expectation.Path}, but operation '{operation.Id}' is now {operation.Method} {operation.Path}."));
            }

            foreach (var expectation in byEndpoint.Where(e => !operation.Documents(e.StatusCode)))
            {
                notices.Add(new OpenApiSyncNotice(OpenApiSyncNoticeKind.UndocumentedStatusCode, expectation.Name, expectation.File,
                    $"'{expectation.Name}' answers {expectation.StatusCode}, which {operation.Method} {operation.Path} does not document ({string.Join(", ", operation.StatusCodes)})."));
            }

            if (byEndpoint.Count > 0 || byId.Count > 0) continue;

            var file = Path.GetFullPath(Path.Combine(outputDirectory, $"{Slug(operation.Tag)}.json"));
            var generated = Generate(operation, document);
            var statusCode = generated["httpResponse"]!["statusCode"]!.GetValue<int>();
            added.Add((new OpenApiSyncAddition(operation.Id, operation.Method, operation.Path, statusCode, file), generated));
        }

        foreach (var expectation in existing.Where(e => e.Method is not null && e.Path is not null && !operations.Any(e.Matches) && !operations.Any(o => o.Id == e.Id)))
        {
            notices.Add(new OpenApiSyncNotice(OpenApiSyncNoticeKind.NotInSpecification, expectation.Name, expectation.File,
                $"'{expectation.Name}' ({expectation.Method} {expectation.Path}) has no matching endpoint in the specification."));
        }

        if (!dryRun)
        {
            foreach (var group in added.GroupBy(a => a.Addition.File))
                Append(group.Key, group.Select(a => a.Expectation).ToList());
        }

        return new OpenApiSyncReport(added.Select(a => a.Addition).ToList(), notices, warnings);
    }

    private static async Task<OpenApiDocument> LoadAsync(string specification, List<string> warnings, CancellationToken cancellationToken)
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        ReadResult result;
        if (Uri.TryCreate(specification, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            result = await OpenApiDocument.LoadAsync(specification, settings, cancellationToken);
        }
        else
        {
            if (!File.Exists(specification)) throw new InvalidOperationException($"OpenAPI specification '{specification}' not found.");
            var format = Path.GetExtension(specification).ToLowerInvariant() is ".yaml" or ".yml" ? "yaml" : "json";
            await using var stream = File.OpenRead(specification);
            result = await OpenApiDocument.LoadAsync(stream, format, settings, cancellationToken);
        }

        if (result.Document is null)
        {
            var errors = result.Diagnostic?.Errors.Select(e => e.ToString()) ?? [];
            throw new InvalidOperationException($"Could not read the OpenAPI specification '{specification}':\n{string.Join('\n', errors)}");
        }

        foreach (var error in result.Diagnostic?.Errors ?? []) warnings.Add($"Specification: {error}");
        return result.Document;
    }

    private static IEnumerable<SpecOperation> Operations(OpenApiDocument document)
    {
        foreach (var (path, item) in document.Paths ?? [])
        {
            foreach (var (method, operation) in item.Operations ?? [])
            {
                var verb = method.Method.ToUpperInvariant();
                var id = string.IsNullOrWhiteSpace(operation.OperationId) ? $"{verb.ToLowerInvariant()}-{Slug(path)}" : operation.OperationId;
                var tag = operation.Tags?.FirstOrDefault()?.Name ?? "default";
                yield return new SpecOperation(id, verb, path, tag, operation);
            }
        }
    }

    private static JsonObject Generate(SpecOperation operation, OpenApiDocument document)
    {
        var (status, response) = PickResponse(operation.Operation);
        var expectation = new JsonObject {
            ["id"] = operation.Id,
            ["httpRequest"] = new JsonObject { ["method"] = operation.Method, ["path"] = operation.Path },
        };
        var httpResponse = new JsonObject { ["statusCode"] = status };

        if (response?.Content is { Count: > 0 } content)
        {
            var (mediaType, media) = content.FirstOrDefault(c => c.Key.Contains("json", StringComparison.OrdinalIgnoreCase)) is { Key: not null } json
                ? json
                : content.First();
            httpResponse["headers"] = new JsonObject { ["Content-Type"] = new JsonArray(mediaType) };
            if (BodyOf(media) is { } body) httpResponse["body"] = body;
        }

        expectation["httpResponse"] = httpResponse;
        return expectation;
    }

    private static (int Status, IOpenApiResponse? Response) PickResponse(OpenApiOperation operation)
    {
        var responses = operation.Responses ?? [];
        var success = responses
            .Select(r => (Status: int.TryParse(r.Key, out var code) ? code : r.Key.ToUpperInvariant() == "2XX" ? 200 : 0, Response: r.Value))
            .Where(r => r.Status is >= 200 and < 300)
            .OrderBy(r => r.Status)
            .FirstOrDefault();
        if (success.Response is not null) return (success.Status, success.Response);
        return responses.TryGetValue("default", out var fallback) ? (200, fallback) : (200, null);
    }

    private static JsonNode? BodyOf(IOpenApiMediaType media)
    {
        if (media.Example is { } example) return example.DeepClone();
        if (media.Examples?.Values.Select(e => e.Value).FirstOrDefault(v => v is not null) is { } named) return named.DeepClone();
        return media.Schema is { } schema ? Skeleton(schema, 0) : null;
    }

    /// <summary>A placeholder value shaped like <paramref name="schema"/>, preferring its examples, defaults and enums.</summary>
    internal static JsonNode? Skeleton(IOpenApiSchema schema, int depth)
    {
#pragma warning disable CS0618 // OpenAPI 3.0 documents still use the singular example.
        if (schema.Example is { } example) return example.DeepClone();
#pragma warning restore CS0618
        if (schema.Examples?.FirstOrDefault() is { } first) return first?.DeepClone();
        if (schema.Default is { } defaultValue) return defaultValue.DeepClone();
        if (schema.Enum?.FirstOrDefault() is { } enumValue) return enumValue.DeepClone();
        if (depth >= MaxSchemaDepth) return null;

        var composed = schema.AllOf is { Count: > 0 } allOf
            ? Merge(allOf.Select(s => Skeleton(s, depth + 1)))
            : (schema.OneOf?.FirstOrDefault() ?? schema.AnyOf?.FirstOrDefault()) is { } option ? Skeleton(option, depth + 1) : null;
        if (composed is not null && schema.Properties is not { Count: > 0 }) return composed;

        var type = schema.Type ?? (schema.Properties is { Count: > 0 } ? JsonSchemaType.Object : schema.Items is not null ? JsonSchemaType.Array : null);
        if (type is null) return composed;

        if (type.Value.HasFlag(JsonSchemaType.Object))
        {
            var result = composed as JsonObject ?? new JsonObject();
            foreach (var (name, property) in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
                result[name] = Skeleton(property, depth + 1);
            return result;
        }
        if (type.Value.HasFlag(JsonSchemaType.Array))
            return schema.Items is { } items ? new JsonArray(Skeleton(items, depth + 1)) : new JsonArray();
        if (type.Value.HasFlag(JsonSchemaType.String))
        {
            return schema.Format switch {
                "date-time" => "2024-01-01T00:00:00Z",
                "date" => "2024-01-01",
                "uuid" => "00000000-0000-0000-0000-000000000000",
                "email" => "user@example.com",
                "uri" => "https://example.com",
                _ => "string",
            };
        }
        if (type.Value.HasFlag(JsonSchemaType.Integer) || type.Value.HasFlag(JsonSchemaType.Number)) return 0;
        if (type.Value.HasFlag(JsonSchemaType.Boolean)) return false;
        return null;
    }

    private static JsonNode? Merge(IEnumerable<JsonNode?> parts)
    {
        JsonObject? merged = null;
        foreach (var part in parts)
        {
            if (part is not JsonObject obj) continue;
            merged ??= new JsonObject();
            foreach (var (name, value) in obj) merged[name] = value?.DeepClone();
        }
        return merged;
    }

    private static List<ExistingExpectation> ReadExisting(string directory, List<string> warnings)
    {
        var result = new List<ExistingExpectation>();
        if (!Directory.Exists(directory)) return result;

        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(file), Lenient);
                var elements = json.RootElement.ValueKind == JsonValueKind.Array ? json.RootElement.EnumerateArray().ToList() : [json.RootElement];
                foreach (var element in elements.Where(e => e.ValueKind == JsonValueKind.Object))
                {
                    var request = Property(element, "httpRequest");
                    var response = Property(element, "httpResponse");
                    result.Add(new ExistingExpectation(
                        Text(Property(element, "id")),
                        Text(Property(request, "method"))?.ToUpperInvariant(),
                        Text(Property(request, "path")),
                        Property(response, "statusCode") is { ValueKind: JsonValueKind.Number } status ? status.GetInt32() : 200,
                        file));
                }
            }
            catch (JsonException exception)
            {
                warnings.Add($"{file}: {exception.Message}");
            }
        }
        return result;
    }

    /// <summary>
    /// Appends <paramref name="expectations"/> to <paramref name="file"/> as text, so the existing content keeps its
    /// formatting and comments; an existing single object becomes the first item of an array.
    /// </summary>
    internal static void Append(string file, IReadOnlyList<JsonObject> expectations)
    {
        var items = string.Join(",\n", expectations.Select(e => Indent(e.ToJsonString(Indented))));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        if (!File.Exists(file) || File.ReadAllText(file).Trim().Length == 0)
        {
            File.WriteAllText(file, $"[\n{items}\n]\n");
            return;
        }

        var text = File.ReadAllText(file).TrimEnd();
        string updated;
        if (text.EndsWith(']'))
        {
            var body = text[..^1].TrimEnd();
            var separator = body.EndsWith('[') || body.EndsWith(',') ? "" : ",";
            updated = $"{body}{separator}\n{items}\n]\n";
        }
        else
        {
            updated = $"[\n{Indent(text)},\n{items}\n]\n";
        }
        File.WriteAllText(file, updated);
    }

    private static string Indent(string text) => string.Join('\n', text.ReplaceLineEndings("\n").Split('\n').Select(line => "  " + line));

    private static JsonElement Property(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return default;
        foreach (var property in element.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        return default;
    }

    private static string? Text(JsonElement element) => element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    private static string Slug(string text)
    {
        var slug = NonAlphanumeric().Replace(text.ToLowerInvariant(), "-").Trim('-');
        return slug.Length == 0 ? "default" : slug;
    }

    /// <summary>A path with parameter names removed and no trailing slash, so <c>/a/{id}</c> equals <c>/a/{orderId}/</c>.</summary>
    internal static string NormalizePath(string path) => Parameter().Replace(path.TrimEnd('/'), "{}") is { Length: > 0 } p ? p : "/";

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex(@"\{[^}/]*\}")]
    private static partial Regex Parameter();

    private sealed record SpecOperation(string Id, string Method, string Path, string Tag, OpenApiOperation Operation)
    {
        public string NormalizedPath { get; } = NormalizePath(Path);

        public IReadOnlyList<string> StatusCodes => Operation.Responses?.Keys.ToList() ?? [];

        // Specifications often document only the errors; then any success code is accepted.
        public bool Documents(int statusCode) =>
            StatusCodes.Any(code => Covers(code, statusCode)) || statusCode is >= 200 and < 300 && !StatusCodes.Any(code => Covers(code, 200) || code.StartsWith('2'));

        private static bool Covers(string code, int statusCode) =>
            code == "default" || code == statusCode.ToString() || code.Length == 3 && code.EndsWith("XX", StringComparison.OrdinalIgnoreCase) && code[0] == statusCode.ToString()[0];
    }

    private sealed record ExistingExpectation(string? Id, string? Method, string? Path, int StatusCode, string File)
    {
        public string Name => Id ?? $"{Method ?? "*"} {Path ?? "*"}";

        public bool Matches(SpecOperation operation) =>
            Method == operation.Method && Path is not null && NormalizePath(Path) == operation.NormalizedPath;
    }
}
