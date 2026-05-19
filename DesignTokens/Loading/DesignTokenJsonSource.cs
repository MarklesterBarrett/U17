using Site.DesignTokens.Defaults;
using Site.DesignTokens.Persistence;
using Site.DesignTokens.Sources;
using System.Text.Json;
using Site.DesignTokens.Themes;

namespace Site.DesignTokens.Loading;

public sealed class DesignTokenJsonSource
{
    private readonly IDesignTokenStarterJsonProvider _starterJsonProvider;
    private readonly IDesignTokenDocumentStore? _documentStore;

    public DesignTokenJsonSource(IDesignTokenStarterJsonProvider starterJsonProvider, IDesignTokenDocumentStore? documentStore = null)
    {
        _starterJsonProvider = starterJsonProvider;
        _documentStore = documentStore;
    }

    public string GetJson(string? importedJson)
    {
        return string.IsNullOrWhiteSpace(importedJson)
            ? _starterJsonProvider.GetStarterJson()
            : importedJson;
    }

    public IReadOnlyList<DesignTokenSource> GetSources(string? importedJson)
    {
        return GetThemeVariants(importedJson)
            .FirstOrDefault(x => x.IsDefault)?.Sources
            ?? [];
    }

    public IReadOnlyList<DesignTokenThemeVariant> GetThemeVariants(string? importedJson)
    {
        var starter = ParseThemeDocument(_starterJsonProvider.GetStarterJson());
        var activeImportedJson = string.IsNullOrWhiteSpace(importedJson)
            ? _documentStore?.GetActive()?.Json
            : importedJson;
        var imported = string.IsNullOrWhiteSpace(activeImportedJson)
            ? ParsedThemeDocument.Empty
            : ParseThemeDocument(activeImportedJson);

        var enabledAliases = DesignTokenThemeVariantCatalog.GetEnabledAliases(imported.EnabledVariantAliases);

        return DesignTokenThemeVariantCatalog.Definitions
            .Select(definition => CreateThemeVariant(definition, starter, imported, enabledAliases.Contains(definition.Alias, StringComparer.Ordinal)))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Alias, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> GetUnsupportedThemeVariantAliases(string? importedJson)
    {
        var activeImportedJson = string.IsNullOrWhiteSpace(importedJson)
            ? _documentStore?.GetActive()?.Json
            : importedJson;

        if (string.IsNullOrWhiteSpace(activeImportedJson))
        {
            return [];
        }

        return ParseThemeDocument(activeImportedJson)
            .UnsupportedVariantAliases
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static DesignTokenThemeVariant CreateThemeVariant(
        DesignTokenThemeVariantCatalog.ThemeVariantDefinition definition,
        ParsedThemeDocument starter,
        ParsedThemeDocument imported,
        bool enabled)
    {
        starter.Variants.TryGetValue(definition.Alias, out var starterVariant);
        imported.Variants.TryGetValue(definition.Alias, out var importedVariant);
        var sources = new List<DesignTokenSource>();

        AddSections(sources, DesignTokenSourceType.Starter, "Starter tokens", starter.BaseSections.BaseJson, DesignTokenSourcePriority.Starter);
        AddSections(sources, DesignTokenSourceType.Component, "Starter component tokens", starter.BaseSections.ComponentJson, DesignTokenSourcePriority.Component);

        if (starterVariant is not null)
        {
            AddSections(sources, DesignTokenSourceType.Starter, $"Starter theme '{definition.Name}'", starterVariant.Sections.BaseJson, DesignTokenSourcePriority.Starter);
            AddSections(sources, DesignTokenSourceType.Component, $"Starter component theme '{definition.Name}'", starterVariant.Sections.ComponentJson, DesignTokenSourcePriority.Component);
        }

        AddSections(sources, DesignTokenSourceType.Imported, "Imported tokens", imported.BaseSections.BaseJson, DesignTokenSourcePriority.Imported);
        AddSections(sources, DesignTokenSourceType.Component, "Imported component tokens", imported.BaseSections.ComponentJson, DesignTokenSourcePriority.Component);

        if (importedVariant is not null)
        {
            AddSections(sources, DesignTokenSourceType.Imported, $"Imported theme '{definition.Name}'", importedVariant.Sections.BaseJson, DesignTokenSourcePriority.Imported);
            AddSections(sources, DesignTokenSourceType.Component, $"Imported component theme '{definition.Name}'", importedVariant.Sections.ComponentJson, DesignTokenSourcePriority.Component);
        }

        return new DesignTokenThemeVariant
        {
            Id = definition.Alias,
            Name = definition.Name,
            Alias = definition.Alias,
            Selector = definition.Selector,
            Sources = sources,
            IsDefault = definition.IsDefault,
            VariantType = definition.VariantType,
            Enabled = definition.IsDefault || enabled
        };
    }

    private static void AddSections(List<DesignTokenSource> sources, DesignTokenSourceType sourceType, string name, string json, int priority)
    {
        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "{}", StringComparison.Ordinal))
        {
            return;
        }

        sources.Add(new DesignTokenSource(sourceType, name, json, priority));
    }

    private static ParsedThemeDocument ParseThemeDocument(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ParsedThemeDocument.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new ParsedThemeDocument(
                    new SplitJsonSections(json, string.Empty),
                    new Dictionary<string, ParsedThemeVariant>(StringComparer.OrdinalIgnoreCase),
                    [],
                    []);
            }

            var baseRoot = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            var variants = new Dictionary<string, ParsedThemeVariant>(StringComparer.OrdinalIgnoreCase);
            var enabledVariantAliases = new List<string>();
            var unsupportedVariantAliases = new List<string>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "themes", StringComparison.Ordinal) &&
                    property.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var variantProperty in property.Value.EnumerateObject())
                    {
                        var canonicalAlias = DesignTokenThemeVariantCatalog.Canonicalize(variantProperty.Name);
                        if (string.IsNullOrWhiteSpace(canonicalAlias))
                        {
                            unsupportedVariantAliases.Add(variantProperty.Name);
                            continue;
                        }

                        var parsedVariant = ParseVariant(canonicalAlias, variantProperty.Value);
                        variants[parsedVariant.Alias] = parsedVariant;
                    }

                    continue;
                }

                if (string.Equals(property.Name, "enabledThemeVariants", StringComparison.Ordinal) &&
                    property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var rawAlias = item.GetString();
                        var canonicalAlias = DesignTokenThemeVariantCatalog.Canonicalize(rawAlias);
                        if (string.IsNullOrWhiteSpace(canonicalAlias))
                        {
                            if (!string.IsNullOrWhiteSpace(rawAlias))
                            {
                                unsupportedVariantAliases.Add(rawAlias);
                            }

                            continue;
                        }

                        enabledVariantAliases.Add(canonicalAlias);
                    }

                    continue;
                }

                if (!string.Equals(property.Name, "themes", StringComparison.Ordinal))
                {
                    baseRoot[property.Name] = property.Value.Clone();
                }
            }

            return new ParsedThemeDocument(
                SplitComponentLayer(JsonSerializer.Serialize(baseRoot)),
                variants,
                enabledVariantAliases,
                unsupportedVariantAliases);
        }
        catch (JsonException)
        {
            return new ParsedThemeDocument(
                new SplitJsonSections(json, string.Empty),
                new Dictionary<string, ParsedThemeVariant>(StringComparer.OrdinalIgnoreCase),
                [],
                []);
        }
    }

    private static ParsedThemeVariant ParseVariant(string alias, JsonElement variantElement)
    {
        var tokenRoot = ExtractVariantTokenRoot(variantElement);
        return new ParsedThemeVariant
        {
            Alias = alias,
            Sections = SplitComponentLayer(JsonSerializer.Serialize(tokenRoot))
        };
    }

    private static JsonElement ExtractVariantTokenRoot(JsonElement variantElement)
    {
        if (variantElement.ValueKind != JsonValueKind.Object)
        {
            using var emptyDocument = JsonDocument.Parse("{}");
            return emptyDocument.RootElement.Clone();
        }

        if (variantElement.TryGetProperty("$tokens", out var explicitTokens) && explicitTokens.ValueKind == JsonValueKind.Object)
        {
            return explicitTokens.Clone();
        }

        if (variantElement.TryGetProperty("tokens", out var tokensProperty) && tokensProperty.ValueKind == JsonValueKind.Object)
        {
            return tokensProperty.Clone();
        }

        var root = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in variantElement.EnumerateObject())
        {
            if (!property.Name.StartsWith('$') && !string.Equals(property.Name, "tokens", StringComparison.Ordinal))
            {
                root[property.Name] = property.Value.Clone();
            }
        }

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(root));
        return document.RootElement.Clone();
    }

    private static SplitJsonSections SplitComponentLayer(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SplitJsonSections(json, string.Empty);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("component", out var componentElement) ||
                componentElement.ValueKind != JsonValueKind.Object)
            {
                return new SplitJsonSections(json, string.Empty);
            }

            var baseRoot = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, "component", StringComparison.Ordinal))
                {
                    baseRoot[property.Name] = property.Value.Clone();
                }
            }

            var componentRoot = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["component"] = componentElement.Clone()
            };

            return new SplitJsonSections(
                JsonSerializer.Serialize(baseRoot),
                JsonSerializer.Serialize(componentRoot));
        }
        catch (JsonException)
        {
            return new SplitJsonSections(json, string.Empty);
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim()
            : null;

    private sealed record SplitJsonSections(string BaseJson, string ComponentJson);

    private sealed record ParsedThemeDocument(
        SplitJsonSections BaseSections,
        IReadOnlyDictionary<string, ParsedThemeVariant> Variants,
        IReadOnlyList<string> EnabledVariantAliases,
        IReadOnlyList<string> UnsupportedVariantAliases)
    {
        public static readonly ParsedThemeDocument Empty = new(
            new SplitJsonSections("{}", string.Empty),
            new Dictionary<string, ParsedThemeVariant>(StringComparer.OrdinalIgnoreCase),
            [],
            []);
    }

    private sealed record ParsedThemeVariant
    {
        public required string Alias { get; init; }

        public required SplitJsonSections Sections { get; init; }
    }
}
