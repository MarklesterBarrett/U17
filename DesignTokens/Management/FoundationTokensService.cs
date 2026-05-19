using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Site.Contentment;
using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Persistence;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Site.DesignTokens.Management;

public interface IFoundationTokensService
{
    FoundationTokensStateResponse GetState();
    DesignTokenDiagnosticsResult Validate(FoundationTokensRequest request);
    DesignTokenPreviewResult Preview(FoundationTokensRequest request);
    DesignTokenManagementResult SaveDraft(FoundationTokensRequest request, string? user);
    DesignTokenManagementResult Publish(FoundationTokensRequest request, string? user);
}

public sealed class FoundationTokensService : IFoundationTokensService
{
    private const string ThemeColorsAlias = "color";
    private const string AdditionalSpacingAlias = "space";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string[] FoundationRootKeys = ["color", "font", "line", "lineHeight", "layout", "radius", "shadow", "space"];
    private static readonly Guid BuiltInColorTypeKey = new("6e2f3941-4c9c-4af7-b31a-8fb331b41249");
    private static readonly Guid CustomColorTypeKey = new("ab9f6723-0dbb-4a0c-8c2f-7bc655f12910");
    private static readonly Guid SpacingTypeKey = new("d81e1092-3ab0-4f0b-9b72-a568d8bfb932");
    private static readonly IReadOnlyDictionary<string, string> FontPresets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["sans"] = "ui-sans-serif, system-ui, sans-serif",
        ["serif"] = "ui-serif, Georgia, Cambria, \"Times New Roman\", Times, serif",
        ["mono"] = "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace"
    };

    private static readonly FoundationTokenDefinition[] Definitions =
    [
        new("colours", "color", "Colours", "color", "color", "color", false, false),
        new("typography", "fontFamilySans", "Body Font", "font.family.sans", "fontFamily", "fontFamily", true, false),
        new("typography", "fontFamilyDisplay", "Display Font", "font.family.display", "fontFamily", "fontFamily", true, false),
        new("typography", "fontSizeSm", "Font Size Small", "font.size.sm", "dimension", "responsive", true, false),
        new("typography", "fontSizeBase", "Font Size Standard", "font.size.base", "dimension", "responsive", true, false),
        new("typography", "fontSizeLg", "Font Size Large", "font.size.lg", "dimension", "responsive", true, false),
        new("typography", "fontSizeXl", "Font Size Heading", "font.size.xl", "dimension", "responsive", true, false),
        new("typography", "fontSize2xl", "Font Size Display", "font.size.2xl", "dimension", "responsive", true, false),
        new("typography", "lineHeightTight", "Line Height Tight", "line.height.tight", "number", "number", true, false),
        new("typography", "lineHeightBase", "Line Height Base", "line.height.base", "number", "number", true, false),
        new("spacing", "spaceXs", "Tight", "space.xs", "dimension", "responsive", true, false),
        new("spacing", "spaceSm", "Compact", "space.sm", "dimension", "responsive", true, false),
        new("spacing", "spaceMd", "Comfortable", "space.md", "dimension", "responsive", true, false),
        new("spacing", "spaceLg", "Spacious", "space.lg", "dimension", "responsive", true, false),
        new("spacing", "spaceXl", "Large", "space.xl", "dimension", "responsive", true, false),
        new("spacing", "space2xl", "Extra Large", "space.2xl", "dimension", "responsive", true, false),
        new("layout", "layoutWidthReading", "Reading Max-width", "layout.width.reading", "dimension", "dimension", true, false),
        new("layout", "layoutWidthContent", "Content Max-width", "layout.width.content", "dimension", "dimension", true, false),
        new("layout", "layoutGutter", "Gutter", "layout.gutter", "dimension", "responsive", true, false),
        new("radius", "radiusSm", "Subtle", "radius.sm", "dimension", "dimension", true, false),
        new("radius", "radiusMd", "Standard", "radius.md", "dimension", "dimension", true, false),
        new("radius", "radiusLg", "Prominent", "radius.lg", "dimension", "dimension", true, false),
        new("radius", "radiusFull", "Full", "radius.full", "dimension", "dimension", true, false),
        new("shadows", "shadowNone", "Flat", "shadow.none", "shadow", "shadowString", true, false),
        new("shadows", "shadowMd", "Raised", "shadow.md", "shadow", "shadowString", true, false),
        new("shadows", "shadowXl", "Lifted", "shadow.xl", "shadow", "shadowString", true, false)
    ];

    private readonly ISiteSettingsResolver _siteSettingsResolver;
    private readonly IContentService _contentService;
    private readonly IDesignTokenManagementService _managementService;

    public FoundationTokensService(
        ISiteSettingsResolver siteSettingsResolver,
        IContentService contentService,
        IDesignTokenManagementService managementService)
    {
        _siteSettingsResolver = siteSettingsResolver;
        _contentService = contentService;
        _managementService = managementService;
    }

    public FoundationTokensStateResponse GetState()
    {
        var themeContent = GetThemeContent();
        var sections = CreateEmptySections();

        if (themeContent is not null)
        {
            PopulateSections(themeContent, sections);
        }

        return new FoundationTokensStateResponse
        {
            ThemeContentId = themeContent?.Id,
            ThemeContentName = themeContent?.Name,
            Sections = sections,
            PaletteOptions = new BuiltInBaseColorDataSource()
                .GetItems(new Dictionary<string, object>())
                .Select(item => new FoundationPaletteOptionResponse
                {
                    Alias = item.Value?.ToString() ?? string.Empty,
                    Label = item.Name ?? item.Value?.ToString() ?? string.Empty,
                    Value = item.Description ?? string.Empty
                })
                .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Summary = new FoundationTokensSummaryResponse
            {
                Colours = sections.Colours.Count,
                Typography = sections.Typography.Count,
                Spacing = sections.Spacing.Count,
                Layout = sections.Layout.Count,
                Radius = sections.Radius.Count,
                Shadows = sections.Shadows.Count
            }
        };
    }

    public DesignTokenDiagnosticsResult Validate(FoundationTokensRequest request) =>
        _managementService.Validate(ComposeJson(request));

    public DesignTokenPreviewResult Preview(FoundationTokensRequest request) =>
        _managementService.PreviewBuild(ComposeJson(request));

    public DesignTokenManagementResult SaveDraft(FoundationTokensRequest request, string? user)
    {
        var themeContent = GetThemeContent() ?? throw new InvalidOperationException("Theme settings content was not found.");
        ApplyTheme(themeContent, request.Sections ?? CreateEmptySections());
        _contentService.Save(themeContent);
        return _managementService.SaveDraft(ComposeJson(request), request.Name, user);
    }

    public DesignTokenManagementResult Publish(FoundationTokensRequest request, string? user)
    {
        var themeContent = GetThemeContent() ?? throw new InvalidOperationException("Theme settings content was not found.");
        ApplyTheme(themeContent, request.Sections ?? CreateEmptySections());
        _contentService.Save(themeContent);

        var saveResult = _managementService.SaveDraft(ComposeJson(request), request.Name, user);
        if (!saveResult.Success || saveResult.Document is null)
        {
            return saveResult;
        }

        var publishResult = _managementService.Activate(saveResult.Document.Id, user);
        if (!publishResult.Success)
        {
            return publishResult;
        }

        var publishOutcome = _contentService.Publish(themeContent, []);
        if (!publishOutcome.Success)
        {
            return new DesignTokenManagementResult
            {
                Success = false,
                Document = publishResult.Document,
                BuildReport = publishResult.BuildReport,
                Tokens = publishResult.Tokens,
                Errors = [new Diagnostics.DesignTokenDiagnostic(Diagnostics.DesignTokenDiagnosticStage.CssWrite, $"Theme settings publish failed for '{themeContent.Name}'.")],
                Warnings = publishResult.Warnings
            };
        }

        return publishResult;
    }

    private IContent? GetThemeContent()
    {
        var published = _siteSettingsResolver.GetThemeSettings();
        return published is null ? null : _contentService.GetById(published.Id);
    }

    private static FoundationSectionsResponse CreateEmptySections() => new()
    {
        Colours = [],
        Typography = [],
        Spacing = [],
        Layout = [],
        Radius = [],
        Shadows = []
    };

    private static void PopulateSections(IContent content, FoundationSectionsResponse sections)
    {
        sections.Colours.AddRange(ReadColourTokens(content));

        foreach (var definition in Definitions.Where(x => !string.Equals(x.Section, "colours", StringComparison.Ordinal)))
        {
            var token = CreateCoreToken(content, definition);
            GetSection(sections, definition.Section).Add(token);
        }

        sections.Spacing.AddRange(ReadAdditionalSpacing(content));
    }

    private static List<FoundationTokenResponse> GetSection(FoundationSectionsResponse sections, string section) =>
        section switch
        {
            "colours" => sections.Colours,
            "typography" => sections.Typography,
            "spacing" => sections.Spacing,
            "layout" => sections.Layout,
            "radius" => sections.Radius,
            "shadows" => sections.Shadows,
            _ => sections.Typography
        };

    private static FoundationTokenResponse CreateCoreToken(IContent content, FoundationTokenDefinition definition)
    {
        object? value = definition.EditorKind switch
        {
            "fontFamily" => ParseFontFamilyValue(content.GetValue<string>(definition.PropertyAlias)),
            "responsive" => ParseResponsiveValue(content.GetValue<string>(definition.PropertyAlias)),
            "number" => ParseNumericString(content.GetValue<string>(definition.PropertyAlias)),
            _ => ParseSimpleValue(content.GetValue<string>(definition.PropertyAlias))
        };

        var rawValue = value switch
        {
            FoundationResponsiveValueResponse responsive => $"{responsive.Mobile} | {responsive.Tablet} | {responsive.Desktop}",
            decimal numberValue => numberValue.ToString(CultureInfo.InvariantCulture),
            _ => value?.ToString() ?? string.Empty
        };

        return new FoundationTokenResponse
        {
            Id = definition.PropertyAlias,
            Section = definition.Section,
            PropertyAlias = definition.PropertyAlias,
            Label = definition.Label,
            Path = definition.TokenPath,
            Type = definition.TokenType,
            EditorKind = definition.EditorKind,
            Value = value,
            RawValue = rawValue,
            ResolvedValue = rawValue,
            CssVariable = ToCssVariable(definition.TokenPath),
            Removable = false
        };
    }

    private static IReadOnlyList<FoundationTokenResponse> ReadColourTokens(IContent content)
    {
        var blockItems = ReadBlockItems(content.GetValue<string>(ThemeColorsAlias));
        var items = new List<FoundationTokenResponse>();
        var index = 0;

        foreach (var block in blockItems)
        {
            var paletteAlias = block.GetValueOrDefault("paletteValue");
            var customLabel = block.GetValueOrDefault("label");
            var customValue = block.GetValueOrDefault("customValue");
            var isBuiltIn = !string.IsNullOrWhiteSpace(paletteAlias);
            var alias = NormalizeAlias(isBuiltIn ? paletteAlias : customLabel);

            if (string.IsNullOrWhiteSpace(alias))
            {
                index += 1;
                continue;
            }

            var value = !string.IsNullOrWhiteSpace(customValue)
                ? customValue
                : BuiltInBaseColorDataSource.TryGetColorValue(paletteAlias ?? string.Empty, out var paletteValue)
                    ? paletteValue
                    : string.Empty;

            items.Add(new FoundationTokenResponse
            {
                Id = $"color-{index}",
                Section = "colours",
                PropertyAlias = ThemeColorsAlias,
                Label = isBuiltIn
                    ? ToFriendlyLabel(paletteAlias)
                    : ToFriendlyLabel(customLabel),
                Path = $"color.{alias}",
                Type = "color",
                EditorKind = isBuiltIn ? "paletteColor" : "customColor",
                Value = value,
                PaletteAlias = paletteAlias,
                Name = customLabel,
                RawValue = value,
                ResolvedValue = value,
                CssVariable = ToCssVariable($"color.{alias}"),
                Removable = !isBuiltIn
            });

            index += 1;
        }

        return items;
    }

    private static IReadOnlyList<FoundationTokenResponse> ReadAdditionalSpacing(IContent content)
    {
        var items = new List<FoundationTokenResponse>();
        var blockItems = ReadBlockItems(content.GetValue<string>(AdditionalSpacingAlias));
        var index = 0;

        foreach (var block in blockItems)
        {
            var name = block.GetValueOrDefault("name");
            if (string.IsNullOrWhiteSpace(name))
            {
                index += 1;
                continue;
            }

            var alias = NormalizeAlias(name);
            var responsive = ParseResponsiveValue(block.GetValueOrDefault("responsiveValues"));
            items.Add(new FoundationTokenResponse
            {
                Id = $"spacing-{index}",
                Section = "spacing",
                PropertyAlias = AdditionalSpacingAlias,
                Label = ToFriendlyLabel(name),
                Path = $"space.{alias}",
                Type = "dimension",
                EditorKind = "responsive",
                Name = name,
                Value = responsive,
                RawValue = $"{responsive.Mobile} | {responsive.Tablet} | {responsive.Desktop}",
                ResolvedValue = $"{responsive.Mobile} | {responsive.Tablet} | {responsive.Desktop}",
                CssVariable = ToCssVariable($"space.{alias}"),
                Removable = true
            });
            index += 1;
        }

        return items;
    }

    private static IEnumerable<Dictionary<string, string>> ReadBlockItems(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            yield break;
        }

        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(rawJson);
            if (!document.RootElement.TryGetProperty("contentData", out var contentData) ||
                contentData.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var contentItem in contentData.EnumerateArray())
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (contentItem.TryGetProperty("values", out var propertyValues) && propertyValues.ValueKind == JsonValueKind.Array)
                {
                    foreach (var property in propertyValues.EnumerateArray())
                    {
                        var alias = property.TryGetProperty("alias", out var aliasElement) ? aliasElement.GetString() : null;
                        if (string.IsNullOrWhiteSpace(alias))
                        {
                            continue;
                        }

                        values[alias] = property.TryGetProperty("value", out var valueElement)
                            ? valueElement.ToString()
                            : string.Empty;
                    }
                }

                yield return values;
            }
        }
        finally
        {
            document?.Dispose();
        }
    }

    private static string ParseSimpleValue(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            if (document.RootElement.TryGetProperty("$value", out var value))
            {
                return value.ToString();
            }
        }
        catch (JsonException)
        {
            return rawJson?.Trim() ?? string.Empty;
        }

        return rawJson?.Trim() ?? string.Empty;
    }

    private static decimal ParseNumericString(string? rawJson)
    {
        var value = ParseSimpleValue(rawJson);
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0m;
    }

    private static FoundationResponsiveValueResponse ParseResponsiveValue(string? rawJson)
    {
        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            try
            {
                using var document = JsonDocument.Parse(rawJson);
                if (document.RootElement.TryGetProperty("$value", out var value))
                {
                    return new FoundationResponsiveValueResponse
                    {
                        Mobile = GetStringProperty(value, "mobile"),
                        Tablet = GetStringProperty(value, "tablet"),
                        Desktop = GetStringProperty(value, "desktop")
                    };
                }
            }
            catch (JsonException)
            {
            }
        }

        return new FoundationResponsiveValueResponse();
    }

    private static string ParseFontFamilyValue(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var preset = GetStringProperty(document.RootElement, "preset");
            var customValue = GetStringProperty(document.RootElement, "customValue");
            if (string.Equals(preset, "custom", StringComparison.OrdinalIgnoreCase))
            {
                return customValue;
            }

            if (!string.IsNullOrWhiteSpace(preset) && FontPresets.TryGetValue(preset, out var presetValue))
            {
                return presetValue;
            }
        }
        catch (JsonException)
        {
            return rawJson.Trim();
        }

        return rawJson.Trim();
    }

    private static string GetStringProperty(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property)
            ? property.ToString()
            : string.Empty;

    private static string NormalizeAlias(string? value) =>
        string.Join("-", (value ?? string.Empty)
            .Trim()
            .Split([' ', '.', '_', '-'], StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

    private static string ToFriendlyLabel(string? value)
    {
        var text = NormalizeAlias(value).Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
    }

    private static string ToCssVariable(string path) => $"--{path.Replace('.', '-')}";

    private static string ComposeJson(FoundationTokensRequest request)
    {
        JsonObject root;

        try
        {
            root = JsonNode.Parse(string.IsNullOrWhiteSpace(request.Json) ? "{}" : request.Json)?.AsObject() ?? new JsonObject();
        }
        catch (JsonException)
        {
            root = new JsonObject();
        }

        foreach (var key in FoundationRootKeys)
        {
            root.Remove(key);
        }

        var sections = request.Sections ?? CreateEmptySections();
        root["color"] = BuildColorJson(sections.Colours);
        root["font"] = BuildFontJson(sections.Typography);
        root["line"] = BuildLineJson(sections.Typography);
        root["space"] = BuildSpacingJson(sections.Spacing);
        root["layout"] = BuildLayoutJson(sections.Layout);
        root["radius"] = BuildRadiusJson(sections.Radius);
        root["shadow"] = BuildShadowJson(sections.Shadows);

        return root.ToJsonString(JsonOptions);
    }

    private static JsonNode BuildColorJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var node = new JsonObject();
        foreach (var token in tokens)
        {
            var alias = token.Path.Split('.').LastOrDefault();
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            node[alias] = CreateTypedToken("color", token.Value?.ToString() ?? string.Empty);
        }

        return node;
    }

    private static JsonNode BuildFontJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var fontFamily = new JsonObject();
        var fontSize = new JsonObject();

        foreach (var token in tokens)
        {
            if (token.Path.StartsWith("font.family.", StringComparison.Ordinal))
            {
                fontFamily[token.Path.Split('.').Last()] = CreateTypedToken("fontFamily", token.Value?.ToString() ?? string.Empty);
            }
            else if (token.Path.StartsWith("font.size.", StringComparison.Ordinal) && token.Value is JsonElement element)
            {
                fontSize[token.Path.Split('.').Last()] = CreateTypedToken("dimension", element);
            }
        }

        return new JsonObject
        {
            ["family"] = fontFamily,
            ["size"] = fontSize
        };
    }

    private static JsonNode BuildLineJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var height = new JsonObject();

        foreach (var token in tokens.Where(x => x.Path.StartsWith("line.height.", StringComparison.Ordinal)))
        {
            var leaf = token.Path.Split('.').Last();
            var value = token.Value switch
            {
                JsonElement element when element.ValueKind == JsonValueKind.Number => element.GetDecimal(),
                decimal number => number,
                _ => decimal.TryParse(token.Value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m
            };
            height[leaf] = CreateTypedToken("number", value);
        }

        return new JsonObject
        {
            ["height"] = height
        };
    }

    private static JsonNode BuildSpacingJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var node = new JsonObject();
        foreach (var token in tokens)
        {
            var alias = token.Path.Split('.').LastOrDefault();
            if (string.IsNullOrWhiteSpace(alias))
            {
                continue;
            }

            if (token.Value is JsonElement element)
            {
                node[alias] = CreateTypedToken("dimension", element);
            }
        }

        return node;
    }

    private static JsonNode BuildLayoutJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var width = new JsonObject();
        JsonNode gutter = CreateTypedToken("dimension", new JsonObject());

        foreach (var token in tokens)
        {
            if (string.Equals(token.Path, "layout.gutter", StringComparison.Ordinal) && token.Value is JsonElement responsive)
            {
                gutter = CreateTypedToken("dimension", responsive);
            }
            else if (token.Path.StartsWith("layout.width.", StringComparison.Ordinal))
            {
                width[token.Path.Split('.').Last()] = CreateTypedToken("dimension", token.Value?.ToString() ?? string.Empty);
            }
        }

        return new JsonObject
        {
            ["gutter"] = gutter,
            ["width"] = width
        };
    }

    private static JsonNode BuildRadiusJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var node = new JsonObject();
        foreach (var token in tokens)
        {
            node[token.Path.Split('.').Last()] = CreateTypedToken("dimension", token.Value?.ToString() ?? string.Empty);
        }

        return node;
    }

    private static JsonNode BuildShadowJson(IEnumerable<FoundationTokenResponse> tokens)
    {
        var node = new JsonObject();
        foreach (var token in tokens)
        {
            node[token.Path.Split('.').Last()] = CreateTypedToken("shadow", ParseShadowString(token.Value?.ToString() ?? string.Empty));
        }

        return node;
    }

    private static JsonNode CreateTypedToken(string type, object? value) => new JsonObject
    {
        ["$type"] = type,
        ["$value"] = JsonSerializer.SerializeToNode(value)
    };

    private static JsonElement ParseShadowString(string value)
    {
        var parts = TokenizeShadow(value);
        var root = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["offsetX"] = parts.Length > 0 ? parts[0] : "0px",
            ["offsetY"] = parts.Length > 1 ? parts[1] : "0px",
            ["blur"] = parts.Length > 2 ? parts[2] : "0px",
            ["spread"] = parts.Length > 3 ? parts[3] : "0px",
            ["color"] = parts.Length > 4 ? string.Join(' ', parts.Skip(4)) : "#00000000"
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(root));
        return document.RootElement.Clone();
    }

    private static string[] TokenizeShadow(string value)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var depth = 0;

        foreach (var character in value)
        {
            if (character == '(')
            {
                depth += 1;
            }
            else if (character == ')' && depth > 0)
            {
                depth -= 1;
            }

            if (char.IsWhiteSpace(character) && depth == 0)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }

    private static void ApplyTheme(IContent content, FoundationSectionsResponse sections)
    {
        content.SetValue("color", CreateBlockListJson(sections.Colours.Select(BuildColorBlock).OfType<BlockPayload>()));

        foreach (var token in sections.Typography.Concat(sections.Layout).Concat(sections.Radius).Concat(sections.Shadows).Concat(sections.Spacing.Where(x => !x.Removable)))
        {
            if (string.IsNullOrWhiteSpace(token.PropertyAlias) || string.Equals(token.PropertyAlias, "space", StringComparison.Ordinal))
            {
                continue;
            }

            content.SetValue(token.PropertyAlias, SerializeTokenValue(token));
        }

        var additionalSpacing = sections.Spacing
            .Where(x => x.Removable)
            .Select(BuildSpacingBlock)
            .OfType<BlockPayload>();

        content.SetValue("space", CreateBlockListJson(additionalSpacing));
    }

    private static string SerializeTokenValue(FoundationTokenResponse token) =>
        token.EditorKind switch
        {
            "fontFamily" => CreateFontFamilyJson(token.Value?.ToString() ?? string.Empty),
            "responsive" => CreateResponsiveJson(token.Value),
            "number" => CreateDimensionJson(token.Value?.ToString() ?? "0"),
            "dimension" => CreateDimensionJson(token.Value?.ToString() ?? string.Empty),
            "shadowString" => token.Value?.ToString() ?? string.Empty,
            _ => token.Value?.ToString() ?? string.Empty
        };

    private static BlockPayload? BuildColorBlock(FoundationTokenResponse token)
    {
        if (string.Equals(token.EditorKind, "paletteColor", StringComparison.Ordinal))
        {
            return new BlockPayload(
                BuiltInColorTypeKey,
                [new PropertyPayload("paletteValue", token.PaletteAlias ?? string.Empty)]);
        }

        return new BlockPayload(
            CustomColorTypeKey,
            [
                new PropertyPayload("label", token.Name ?? string.Empty),
                new PropertyPayload("customValue", token.Value?.ToString() ?? string.Empty)
            ]);
    }

    private static BlockPayload? BuildSpacingBlock(FoundationTokenResponse token)
    {
        return new BlockPayload(
            SpacingTypeKey,
            [
                new PropertyPayload("name", token.Name ?? token.Label),
                new PropertyPayload("responsiveValues", CreateResponsiveJson(token.Value))
            ]);
    }

    private static string CreateFontFamilyJson(string value)
    {
        foreach (var preset in FontPresets)
        {
            if (string.Equals(preset.Value, value, StringComparison.Ordinal))
            {
                return JsonSerializer.Serialize(new { preset = preset.Key, customValue = string.Empty });
            }
        }

        return JsonSerializer.Serialize(new { preset = "custom", customValue = value });
    }

    private static string CreateResponsiveJson(object? value)
    {
        var responsive = value switch
        {
            JsonElement json => new FoundationResponsiveValueResponse
            {
                Mobile = GetJsonElementString(json, "mobile"),
                Tablet = GetJsonElementString(json, "tablet"),
                Desktop = GetJsonElementString(json, "desktop")
            },
            FoundationResponsiveValueResponse typed => typed,
            _ => new FoundationResponsiveValueResponse()
        };

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["$type"] = "dimension",
            ["$value"] = new Dictionary<string, string?>
            {
                ["mobile"] = responsive.Mobile,
                ["tablet"] = responsive.Tablet,
                ["desktop"] = responsive.Desktop
            }
        });
    }

    private static string GetJsonElementString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property.ToString()
            : string.Empty;

    private static string CreateDimensionJson(string value) => JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        ["$type"] = "dimension",
        ["$value"] = value
    });

    private static string CreateBlockListJson(IEnumerable<BlockPayload> payloads)
    {
        var blocks = payloads.ToArray();
        if (blocks.Length == 0)
        {
            return string.Empty;
        }

        var contentData = new List<object>();
        var expose = new List<object>();
        var layout = new List<object>();

        foreach (var payload in blocks)
        {
            var key = Guid.NewGuid();
            contentData.Add(new
            {
                contentTypeKey = payload.ContentTypeKey,
                key,
                values = payload.Values.Select(value => new
                {
                    alias = value.Alias,
                    culture = (string?)null,
                    segment = (string?)null,
                    editorAlias = (string?)null,
                    value = value.Value
                })
            });
            expose.Add(new
            {
                contentKey = key,
                culture = (string?)null,
                segment = (string?)null
            });
            layout.Add(new
            {
                contentKey = key,
                contentUdi = (string?)null,
                settingsKey = (Guid?)null,
                settingsUdi = (string?)null
            });
        }

        return JsonSerializer.Serialize(new
        {
            contentData,
            settingsData = Array.Empty<object>(),
            expose,
            Layout = new Dictionary<string, object>
            {
                ["Umbraco.BlockList"] = layout
            }
        });
    }

    private sealed record FoundationTokenDefinition(
        string Section,
        string PropertyAlias,
        string Label,
        string TokenPath,
        string TokenType,
        string EditorKind,
        bool Core,
        bool Removable);

    private sealed record BlockPayload(Guid ContentTypeKey, IReadOnlyList<PropertyPayload> Values);
    private sealed record PropertyPayload(string Alias, object? Value);
}

public sealed class FoundationTokensRequest
{
    public string? Name { get; init; }
    public string? Json { get; init; }
    public FoundationSectionsResponse? Sections { get; init; }
}

public sealed class FoundationTokensStateResponse
{
    public int? ThemeContentId { get; init; }
    public string? ThemeContentName { get; init; }
    public required FoundationSectionsResponse Sections { get; init; }
    public required IReadOnlyList<FoundationPaletteOptionResponse> PaletteOptions { get; init; }
    public required FoundationTokensSummaryResponse Summary { get; init; }
}

public sealed class FoundationTokensSummaryResponse
{
    public int Colours { get; init; }
    public int Typography { get; init; }
    public int Spacing { get; init; }
    public int Layout { get; init; }
    public int Radius { get; init; }
    public int Shadows { get; init; }
}

public sealed class FoundationPaletteOptionResponse
{
    public required string Alias { get; init; }
    public required string Label { get; init; }
    public required string Value { get; init; }
}

public sealed class FoundationSectionsResponse
{
    public required List<FoundationTokenResponse> Colours { get; init; }
    public required List<FoundationTokenResponse> Typography { get; init; }
    public required List<FoundationTokenResponse> Spacing { get; init; }
    public required List<FoundationTokenResponse> Layout { get; init; }
    public required List<FoundationTokenResponse> Radius { get; init; }
    public required List<FoundationTokenResponse> Shadows { get; init; }
}

public sealed class FoundationTokenResponse
{
    public required string Id { get; init; }
    public required string Section { get; init; }
    public required string PropertyAlias { get; init; }
    public required string Label { get; init; }
    public required string Path { get; init; }
    public required string Type { get; init; }
    public required string EditorKind { get; init; }
    public object? Value { get; init; }
    public string? Name { get; init; }
    public string? PaletteAlias { get; init; }
    public required string RawValue { get; init; }
    public required string ResolvedValue { get; init; }
    public required string CssVariable { get; init; }
    public bool Removable { get; init; }
}

public sealed class FoundationResponsiveValueResponse
{
    public string Mobile { get; init; } = string.Empty;
    public string Tablet { get; init; } = string.Empty;
    public string Desktop { get; init; } = string.Empty;
}
