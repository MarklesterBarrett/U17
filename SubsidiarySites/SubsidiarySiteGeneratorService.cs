using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.StaticFiles;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Site.DesignTokens;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Site.SubsidiarySites;

public interface ISubsidiarySiteThemeService
{
    string CreateDefaultThemeJson();
    ThemeImportResult Validate(string? json, bool useDefaultsWhenEmpty);
}

public interface ISubsidiarySiteGeneratorService
{
    Task<SiteGenerationResponse> GenerateAsync(SiteGenerationRequest request);
}

internal sealed class SubsidiarySiteThemeService : ISubsidiarySiteThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string[] CoreSpacingKeys = ["xs", "sm", "md", "lg", "xl", "2xl"];
    private static readonly IReadOnlyDictionary<string, string> PrimitiveColorDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["blue-500"] = "#0f766e",
        ["blue-600"] = "#115e59",
        ["slate-50"] = "#f8fafc",
        ["white"] = "#ffffff",
        ["cyan-50"] = "#ecfeff",
        ["slate-900"] = "#0f172a",
        ["slate-950"] = "#020617",
        ["slate-600"] = "#475569",
        ["slate-300"] = "#cbd5e1",
        ["slate-200"] = "#e2e8f0",
        ["slate-500"] = "#64748b",
        ["green-700"] = "#15803d",
        ["amber-700"] = "#b45309",
        ["red-700"] = "#b91c1c",
        ["blue-700"] = "#1d4ed8"
    };
    private static readonly IReadOnlyDictionary<string, string> SemanticColorDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["button-primary-background"] = "blue-500",
        ["button-primary-hover-background"] = "blue-600",
        ["button-primary-text"] = "white",
        ["button-primary-hover-text"] = "white",
        ["button-secondary-background"] = "white",
        ["button-secondary-hover-background"] = "cyan-50",
        ["button-secondary-text"] = "slate-900",
        ["button-secondary-hover-text"] = "slate-950",
        ["button-disabled-background"] = "cyan-50",
        ["button-disabled-text"] = "slate-600",
        ["button-disabled-border"] = "slate-200",
        ["link-color"] = "blue-500",
        ["link-hover-color"] = "blue-600",
        ["link-active-color"] = "blue-500",
        ["link-visited-color"] = "blue-600",
        ["link-focus-color"] = "blue-500",
        ["focus-ring-color"] = "blue-500",
        ["text-body-color"] = "slate-900",
        ["text-heading-color"] = "slate-950",
        ["text-muted-color"] = "slate-600",
        ["text-inverted-color"] = "slate-50",
        ["surface-page-background"] = "slate-50",
        ["surface-section-background"] = "cyan-50",
        ["surface-card-background"] = "white",
        ["surface-inverted-background"] = "slate-900",
        ["border-default-color"] = "slate-300",
        ["border-subtle-color"] = "slate-200",
        ["border-strong-color"] = "slate-500",
        ["input-background"] = "white",
        ["input-text"] = "slate-900",
        ["input-border"] = "slate-300",
        ["input-border-focus"] = "blue-500",
        ["input-placeholder"] = "slate-600",
        ["validation-error-color"] = "red-700",
        ["validation-success-color"] = "green-700",
        ["feedback-success-color"] = "green-700",
        ["feedback-warning-color"] = "amber-700",
        ["feedback-error-color"] = "red-700",
        ["feedback-info-color"] = "blue-700",
        ["header-background"] = "slate-50",
        ["header-text-color"] = "slate-900",
        ["header-link-color"] = "slate-900",
        ["header-link-hover-color"] = "blue-500",
        ["footer-background"] = "slate-900",
        ["footer-text-color"] = "slate-50",
        ["footer-link-color"] = "slate-50",
        ["footer-link-hover-color"] = "blue-500"
    };
    private static readonly IReadOnlyDictionary<string, string> SemanticValueDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["card-radius"] = "radius-md",
        ["card-shadow"] = "shadow-md",
        ["section-spacing"] = "space-xl",
        ["container-width"] = "layout-width-content"
    };

    public string CreateDefaultThemeJson()
    {
        var root = new JsonObject
        {
            ["$schema"] = "https://www.designtokens.org/tr/drafts/format/",
            ["color"] = PrimitiveColorDefaults.ToJsonObject(entry => CreateToken(entry, "color")),
            ["space"] = new JsonObject
            {
                ["xs"] = CreateResponsiveToken("4px", "8px", "8px"),
                ["sm"] = CreateResponsiveToken("8px", "12px", "12px"),
                ["md"] = CreateResponsiveToken("16px", "20px", "24px"),
                ["lg"] = CreateResponsiveToken("24px", "32px", "40px"),
                ["xl"] = CreateResponsiveToken("32px", "48px", "56px"),
                ["2xl"] = CreateResponsiveToken("48px", "64px", "80px")
            },
            ["font"] = new JsonObject
            {
                ["family"] = new JsonObject
                {
                    ["sans"] = new JsonObject
                    {
                        ["$type"] = "fontFamily",
                        ["$value"] = new JsonObject
                        {
                            ["preset"] = "sans",
                            ["customValue"] = ""
                        }
                    },
                    ["display"] = new JsonObject
                    {
                        ["$type"] = "fontFamily",
                        ["$value"] = new JsonObject
                        {
                            ["preset"] = "serif",
                            ["customValue"] = ""
                        }
                    }
                },
                ["size"] = new JsonObject
                {
                    ["sm"] = CreateResponsiveToken("14px", "14px", "15px"),
                    ["base"] = CreateResponsiveToken("16px", "16px", "18px"),
                    ["lg"] = CreateResponsiveToken("20px", "22px", "24px"),
                    ["xl"] = CreateResponsiveToken("28px", "32px", "36px"),
                    ["2xl"] = CreateResponsiveToken("40px", "48px", "56px")
                }
            },
            ["lineHeight"] = new JsonObject
            {
                ["tight"] = CreateToken("1.2", "number"),
                ["base"] = CreateToken("1.6", "number")
            },
            ["layout"] = new JsonObject
            {
                ["gutter"] = CreateResponsiveToken("20px", "32px", "40px"),
                ["width"] = new JsonObject
                {
                    ["content"] = CreateToken("1200px", "dimension"),
                    ["reading"] = CreateToken("760px", "dimension")
                }
            },
            ["radius"] = new JsonObject
            {
                ["sm"] = CreateToken("6px", "dimension"),
                ["md"] = CreateToken("12px", "dimension"),
                ["lg"] = CreateToken("24px", "dimension"),
                ["full"] = CreateToken("999px", "dimension")
            },
            ["shadow"] = new JsonObject
            {
                ["none"] = CreateToken("0 0 #0000", "shadow"),
                ["md"] = CreateToken("0 2px 8px rgb(0 0 0 / 0.08)", "shadow"),
                ["xl"] = CreateToken("0 12px 32px rgb(0 0 0 / 0.16)", "shadow")
            },
            ["semantic"] = new JsonObject
            {
                ["colors"] = SemanticColorDefaults.ToJsonObject(CreateReferenceToken),
                ["values"] = SemanticValueDefaults.ToJsonObject(CreateReferenceToken),
                ["direct"] = new JsonObject
                {
                    ["focus-ring-width"] = CreateToken("2px", "dimension"),
                    ["focus-ring-offset"] = CreateToken("2px", "dimension")
                }
            }
        };

        return root.ToJsonString(JsonOptions);
    }

    public ThemeImportResult Validate(string? json, bool useDefaultsWhenEmpty)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var theme = CreateDefaultTheme();

        if (string.IsNullOrWhiteSpace(json))
        {
            if (useDefaultsWhenEmpty)
            {
                warnings.Add("No design token JSON supplied. Default theme tokens will be used.");
            }
            else
            {
                errors.Add("Design token JSON is required.");
            }

            return new ThemeImportResult(theme, errors, warnings);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add($"Design token JSON is invalid: {ex.Message}");
            return new ThemeImportResult(theme, errors, warnings);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Design token JSON must be an object.");
                return new ThemeImportResult(theme, errors, warnings);
            }

            var root = document.RootElement;

            var colors = new Dictionary<string, PrimitiveColorToken>(theme.Colors, StringComparer.OrdinalIgnoreCase);
            OverlayColorSection(root, colors, errors);

            var coreSpacing = new Dictionary<string, ResponsiveTokenValue>(theme.CoreSpacing, StringComparer.OrdinalIgnoreCase);
            OverlayResponsiveSection(root, "space", CoreSpacingKeys, coreSpacing, errors);

            var additionalSpacing = new List<AdditionalSpacingToken>(theme.AdditionalSpacing);
            OverlayAdditionalSpacing(root, additionalSpacing, errors);

            var sansFont = OverlayFontFamily(root, "sans", theme.SansFont, warnings);
            var displayFont = OverlayFontFamily(root, "display", theme.DisplayFont, warnings);

            var fontSizes = new Dictionary<string, ResponsiveTokenValue>(theme.FontSizes, StringComparer.OrdinalIgnoreCase);
            OverlayResponsiveSection(GetChild(root, "font"), "size", CoreSpacingKeys, fontSizes, errors);

            var lineHeights = new Dictionary<string, string>(theme.LineHeights, StringComparer.OrdinalIgnoreCase);
            OverlaySimpleValueSection(root, "lineHeight", ["tight", "base"], lineHeights, errors);

            var layoutGutter = theme.LayoutGutter;
            OverlaySingleResponsiveValue(root, "layout", "gutter", ref layoutGutter, errors);

            var layoutWidths = new Dictionary<string, string>(theme.LayoutWidths, StringComparer.OrdinalIgnoreCase);
            OverlaySimpleValueSection(GetChild(root, "layout"), "width", ["content", "reading"], layoutWidths, errors);

            var radius = new Dictionary<string, string>(theme.Radius, StringComparer.OrdinalIgnoreCase);
            OverlaySimpleValueSection(root, "radius", ["sm", "md", "lg", "full"], radius, errors);

            var shadow = new Dictionary<string, string>(theme.Shadow, StringComparer.OrdinalIgnoreCase);
            OverlaySimpleValueSection(root, "shadow", ["none", "md", "xl"], shadow, errors);

            var semanticColors = new Dictionary<string, string>(theme.SemanticColorAssignments, StringComparer.OrdinalIgnoreCase);
            OverlayReferenceSection(GetChild(root, "semantic"), "colors", SemanticColorDefaults.Keys.ToArray(), semanticColors, errors);

            var semanticValues = new Dictionary<string, string>(theme.SemanticValueAssignments, StringComparer.OrdinalIgnoreCase);
            OverlayReferenceSection(GetChild(root, "semantic"), "values", SemanticValueDefaults.Keys.ToArray(), semanticValues, errors);

            var directValues = new Dictionary<string, string>(theme.DirectValues, StringComparer.OrdinalIgnoreCase);
            OverlaySimpleValueSection(GetChild(root, "semantic"), "direct", ["focus-ring-width", "focus-ring-offset"], directValues, errors);

            var additionalValueTokens = new List<AdditionalValueToken>(theme.AdditionalValueTokens);
            OverlayAdditionalValueTokens(GetChild(root, "semantic"), additionalValueTokens, errors);

            foreach (var assignment in semanticColors)
            {
                if (!colors.ContainsKey(assignment.Value))
                {
                    errors.Add($"Semantic color '{assignment.Key}' references missing primitive color '{assignment.Value}'.");
                }
            }

            var knownValueAliases = CreateKnownValueAliases(coreSpacing, additionalSpacing, radius, shadow, layoutWidths);
            knownValueAliases.Add("layout-gutter");

            foreach (var token in additionalValueTokens)
            {
                knownValueAliases.Add(token.Alias);
            }

            foreach (var assignment in semanticValues)
            {
                if (!knownValueAliases.Contains(assignment.Value))
                {
                    errors.Add($"Semantic value '{assignment.Key}' references unknown token '{assignment.Value}'.");
                }
            }

            theme = new SiteThemeImport(
                colors,
                coreSpacing,
                additionalSpacing,
                sansFont,
                displayFont,
                fontSizes,
                lineHeights,
                layoutGutter,
                layoutWidths,
                radius,
                shadow,
                semanticColors,
                semanticValues,
                directValues,
                additionalValueTokens);
        }

        return new ThemeImportResult(theme, errors, warnings);
    }

    private static SiteThemeImport CreateDefaultTheme() => new(
        PrimitiveColorDefaults.ToDictionary(
            entry => entry.Key,
            entry => new PrimitiveColorToken(entry.Key, entry.Value),
            StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, ResponsiveTokenValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["xs"] = new("4px", "8px", "8px"),
            ["sm"] = new("8px", "12px", "12px"),
            ["md"] = new("16px", "20px", "24px"),
            ["lg"] = new("24px", "32px", "40px"),
            ["xl"] = new("32px", "48px", "56px"),
            ["2xl"] = new("48px", "64px", "80px")
        },
        [],
        new FontFamilyToken("sans", string.Empty),
        new FontFamilyToken("serif", string.Empty),
        new Dictionary<string, ResponsiveTokenValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["sm"] = new("14px", "14px", "15px"),
            ["base"] = new("16px", "16px", "18px"),
            ["lg"] = new("20px", "22px", "24px"),
            ["xl"] = new("28px", "32px", "36px"),
            ["2xl"] = new("40px", "48px", "56px")
        },
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tight"] = "1.2",
            ["base"] = "1.6"
        },
        new ResponsiveTokenValue("20px", "32px", "40px"),
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["content"] = "1200px",
            ["reading"] = "760px"
        },
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sm"] = "6px",
            ["md"] = "12px",
            ["lg"] = "24px",
            ["full"] = "999px"
        },
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = "0 0 #0000",
            ["md"] = "0 2px 8px rgb(0 0 0 / 0.08)",
            ["xl"] = "0 12px 32px rgb(0 0 0 / 0.16)"
        },
        new Dictionary<string, string>(SemanticColorDefaults, StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, string>(SemanticValueDefaults, StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["focus-ring-width"] = "2px",
            ["focus-ring-offset"] = "2px"
        },
        []);

    private static void OverlayColorSection(JsonElement root, Dictionary<string, PrimitiveColorToken> colors, List<string> errors)
    {
        var colorSection = GetChild(root, "color");
        if (colorSection.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in colorSection.EnumerateObject())
        {
            if (!TryReadTokenString(property.Value, out var value))
            {
                errors.Add($"Color token '{property.Name}' must contain a string '$value'.");
                continue;
            }

            colors[property.Name] = new PrimitiveColorToken(property.Name, value);
        }
    }

    private static void OverlayResponsiveSection(
        JsonElement root,
        string sectionName,
        IReadOnlyList<string> keys,
        Dictionary<string, ResponsiveTokenValue> target,
        List<string> errors)
    {
        var section = GetChild(root, sectionName);
        if (section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var key in keys)
        {
            if (!section.TryGetProperty(key, out var property))
            {
                continue;
            }

            if (!TryReadResponsiveToken(property, out var value))
            {
                errors.Add($"Responsive token '{sectionName}.{key}' must contain '$value.mobile', '$value.tablet', and '$value.desktop' string values.");
                continue;
            }

            target[key] = value;
        }
    }

    private static void OverlaySingleResponsiveValue(
        JsonElement root,
        string sectionName,
        string propertyName,
        ref ResponsiveTokenValue target,
        List<string> errors)
    {
        var section = GetChild(root, sectionName);
        if (section.ValueKind != JsonValueKind.Object || !section.TryGetProperty(propertyName, out var property))
        {
            return;
        }

        if (!TryReadResponsiveToken(property, out var value))
        {
            errors.Add($"Responsive token '{sectionName}.{propertyName}' must contain '$value.mobile', '$value.tablet', and '$value.desktop' string values.");
            return;
        }

        target = value;
    }

    private static void OverlaySimpleValueSection(
        JsonElement root,
        string sectionName,
        IReadOnlyList<string> keys,
        Dictionary<string, string> target,
        List<string> errors)
    {
        var section = GetChild(root, sectionName);
        if (section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var key in keys)
        {
            if (!section.TryGetProperty(key, out var property))
            {
                continue;
            }

            if (!TryReadTokenString(property, out var value))
            {
                errors.Add($"Token '{sectionName}.{key}' must contain a string '$value'.");
                continue;
            }

            target[key] = value;
        }
    }

    private static void OverlayReferenceSection(
        JsonElement root,
        string sectionName,
        IReadOnlyList<string> keys,
        Dictionary<string, string> target,
        List<string> errors)
    {
        var section = GetChild(root, sectionName);
        if (section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var key in keys)
        {
            if (!section.TryGetProperty(key, out var property))
            {
                continue;
            }

            if (!TryReadTokenString(property, out var value))
            {
                errors.Add($"Reference token '{sectionName}.{key}' must contain a string '$value'.");
                continue;
            }

            target[key] = value;
        }
    }

    private static void OverlayAdditionalSpacing(JsonElement root, List<AdditionalSpacingToken> target, List<string> errors)
    {
        var spaceSection = GetChild(root, "space");
        if (spaceSection.ValueKind != JsonValueKind.Object || !spaceSection.TryGetProperty("additional", out var additional))
        {
            return;
        }

        if (additional.ValueKind != JsonValueKind.Object)
        {
            errors.Add("space.additional must be an object keyed by token name.");
            return;
        }

        target.Clear();

        foreach (var property in additional.EnumerateObject())
        {
            if (!TryReadResponsiveToken(property.Value, out var value))
            {
                errors.Add($"Additional spacing token '{property.Name}' must contain '$value.mobile', '$value.tablet', and '$value.desktop' string values.");
                continue;
            }

            target.Add(new AdditionalSpacingToken(property.Name, value));
        }
    }

    private static void OverlayAdditionalValueTokens(JsonElement root, List<AdditionalValueToken> target, List<string> errors)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("additional", out var additional))
        {
            return;
        }

        if (additional.ValueKind != JsonValueKind.Array)
        {
            errors.Add("semantic.additional must be an array.");
            return;
        }

        target.Clear();

        foreach (var item in additional.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Each semantic.additional item must be an object.");
                continue;
            }

            var alias = GetString(item, "alias");
            var label = GetString(item, "label");

            if (!TryReadTokenString(item, out var value) || string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(label))
            {
                errors.Add("Each semantic.additional item must include alias, label, and string '$value'.");
                continue;
            }

            target.Add(new AdditionalValueToken(alias, label, value));
        }
    }

    private static FontFamilyToken OverlayFontFamily(JsonElement root, string key, FontFamilyToken fallback, List<string> warnings)
    {
        var fontSection = GetChild(GetChild(root, "font"), "family");
        if (fontSection.ValueKind != JsonValueKind.Object || !fontSection.TryGetProperty(key, out var token))
        {
            return fallback;
        }

        var value = token.TryGetProperty("$value", out var inner) ? inner : token;
        if (value.ValueKind != JsonValueKind.Object)
        {
            warnings.Add($"font.family.{key} must be an object. Default will be used.");
            return fallback;
        }

        var preset = GetString(value, "preset");
        var customValue = GetString(value, "customValue");

        if (string.IsNullOrWhiteSpace(preset))
        {
            warnings.Add($"font.family.{key}.preset is missing. Default will be used.");
            return fallback;
        }

        return new FontFamilyToken(preset, customValue);
    }

    private static HashSet<string> CreateKnownValueAliases(
        IReadOnlyDictionary<string, ResponsiveTokenValue> coreSpacing,
        IReadOnlyList<AdditionalSpacingToken> additionalSpacing,
        IReadOnlyDictionary<string, string> radius,
        IReadOnlyDictionary<string, string> shadow,
        IReadOnlyDictionary<string, string> layoutWidths)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in coreSpacing.Keys)
        {
            result.Add($"space-{key}");
        }

        foreach (var item in additionalSpacing)
        {
            result.Add($"space-{ToKebabCase(item.Name)}");
        }

        foreach (var key in radius.Keys)
        {
            result.Add($"radius-{key}");
        }

        foreach (var key in shadow.Keys)
        {
            result.Add($"shadow-{key}");
        }

        foreach (var key in layoutWidths.Keys)
        {
            result.Add($"layout-width-{key}");
        }

        result.Add("radius-none");
        return result;
    }

    private static JsonObject CreateToken(string value, string type) => new()
    {
        ["$type"] = type,
        ["$value"] = value
    };

    private static JsonObject CreateResponsiveToken(string mobile, string tablet, string desktop) => new()
    {
        ["$type"] = "dimension",
        ["$value"] = new JsonObject
        {
            ["mobile"] = mobile,
            ["tablet"] = tablet,
            ["desktop"] = desktop
        }
    };

    private static JsonObject CreateReferenceToken(string value) => new()
    {
        ["$type"] = "token",
        ["$value"] = value
    };

    private static JsonElement GetChild(JsonElement root, string propertyName)
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var value)
            ? value
            : default;
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static bool TryReadTokenString(JsonElement token, out string value)
    {
        value = string.Empty;
        var source = token;

        if (token.ValueKind == JsonValueKind.Object && token.TryGetProperty("$value", out var wrapped))
        {
            source = wrapped;
        }

        if (source.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = source.GetString()?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadResponsiveToken(JsonElement token, out ResponsiveTokenValue value)
    {
        value = new ResponsiveTokenValue(string.Empty, string.Empty, string.Empty);
        var source = token;

        if (token.ValueKind == JsonValueKind.Object && token.TryGetProperty("$value", out var wrapped))
        {
            source = wrapped;
        }

        if (source.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var mobile = GetString(source, "mobile");
        var tablet = GetString(source, "tablet");
        var desktop = GetString(source, "desktop");

        if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(tablet) || string.IsNullOrWhiteSpace(desktop))
        {
            return false;
        }

        value = new ResponsiveTokenValue(mobile, tablet, desktop);
        return true;
    }

    private static string ToKebabCase(string value)
    {
        return string.Join(
            "-",
            value.Trim()
                .ToLowerInvariant()
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries));
    }
}

internal sealed class SubsidiarySiteGeneratorService : ISubsidiarySiteGeneratorService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDesignTokenCssGenerator _designTokenCssGenerator;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly ISubsidiarySiteThemeService _themeService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public SubsidiarySiteGeneratorService(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IDesignTokenCssGenerator designTokenCssGenerator,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGeneratorCollection,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        ISubsidiarySiteThemeService themeService,
        IWebHostEnvironment webHostEnvironment)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _designTokenCssGenerator = designTokenCssGenerator;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _themeService = themeService;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<SiteGenerationResponse> GenerateAsync(SiteGenerationRequest request)
    {
        var steps = CreateSteps();
        var createdItems = new List<string>();
        var userDetails = new List<string>();
        var manualActions = new List<string>();
        var warnings = new List<string>();

        CompleteStep(steps, "validate-inputs", new[]
        {
            "Checking site name uniqueness.",
            "Checking design token schema."
        });

        var validationErrors = ValidateSiteName(request.SiteName);
        var rootNameExists = _contentService.GetRootContent()
            .Any(x => string.Equals(x.Name, request.SiteName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (rootNameExists)
        {
            validationErrors.Add($"A root node named '{request.SiteName.Trim()}' already exists.");
        }

        var tenantKey = CreateTenantKey(request.SiteName);
        var tenantKeyExists = _contentService.GetRootContent()
            .Any(x => string.Equals(x.GetValue<string>("tenantKey")?.Trim(), tenantKey, StringComparison.OrdinalIgnoreCase));

        if (tenantKeyExists)
        {
            validationErrors.Add($"Derived tenant key '{tenantKey}' already exists. Use a different site name.");
        }

        var themeValidation = _themeService.Validate(request.DesignTokenJson, useDefaultsWhenEmpty: true);
        warnings.AddRange(themeValidation.Warnings);

        if (validationErrors.Count != 0 || !themeValidation.IsValid)
        {
            FailStep(steps, "validate-inputs", validationErrors.Concat(themeValidation.Errors));
            return BuildResponse(false, request.SiteName.Trim(), tenantKey, null, steps, createdItems, userDetails, manualActions, warnings);
        }

        var requiredTypes = new[]
        {
            "tenantSite", "page", "settings", "settingsIdentity", "settingsHeader",
            "settingsFooter", "settingsTheme", "settingsStyles"
        };

        var missingTypes = requiredTypes.Where(alias => _contentTypeService.Get(alias) is null).ToArray();
        if (missingTypes.Length != 0)
        {
            FailStep(steps, "validate-inputs", missingTypes.Select(x => $"Missing content type '{x}'. Import uSync before using the generator."));
            return BuildResponse(false, request.SiteName.Trim(), tenantKey, null, steps, createdItems, userDetails, manualActions, warnings);
        }

        CompleteStep(steps, "validate-inputs", [$"Site name '{request.SiteName.Trim()}' is available.", $"Tenant key '{tenantKey}' is available."]);

        IContent? root = null;
        IContent? home = null;
        IContent? about = null;
        IContent? siteSettings = null;
        IContent? identity = null;
        IContent? header = null;
        IContent? footer = null;
        IContent? themeSettings = null;
        IContent? themeRoles = null;
        LogoAssignmentResult logoAssignment = new(null, null, null, null, []);

        try
        {
            root = _contentService.Create(request.SiteName.Trim(), -1, "tenantSite");
            root.SetValue("tenantKey", tenantKey);
            SaveOrThrow(root);
            createdItems.Add(request.SiteName.Trim());
            CompleteStep(steps, "create-root-site", [$"Created root node '{root.Name}' with tenant key '{tenantKey}'."]);

            home = _contentService.Create("Home", root, "page");
            SaveOrThrow(home);
            createdItems.Add("Home");
            CompleteStep(steps, "create-home-page", ["Created Home page."]);

            about = _contentService.Create("About", root, "page");
            SaveOrThrow(about);
            createdItems.Add("About");
            CompleteStep(steps, "create-about-page", ["Created About page."]);

            siteSettings = _contentService.Create("Settings", root, "settings");
            SaveOrThrow(siteSettings);
            createdItems.Add("Site Settings");
            CompleteStep(steps, "create-site-settings", ["Created Site Settings node."]);

            identity = _contentService.Create("Identity", siteSettings, "settingsIdentity");
            identity.SetValue("siteName", request.SiteName.Trim());
            identity.SetValue("siteTitle", "{pageName} | {siteName}");
            SaveOrThrow(identity);
            createdItems.Add("Identity");
            CompleteStep(steps, "create-identity-settings", ["Created Identity settings."]);

            header = _contentService.Create("Header", siteSettings, "settingsHeader");
            SaveOrThrow(header);
            createdItems.Add("Header");
            CompleteStep(steps, "create-header-settings", ["Created Header settings."]);

            footer = _contentService.Create("Footer", siteSettings, "settingsFooter");
            SaveOrThrow(footer);
            createdItems.Add("Footer");
            CompleteStep(steps, "create-footer-settings", ["Created Footer settings."]);

            themeSettings = _contentService.Create("Theme", siteSettings, "settingsTheme");
            ApplyThemeSettings(themeSettings, themeValidation.Theme);
            SaveOrThrow(themeSettings);

            PublishOrThrow(root);
            PublishOrThrow(siteSettings);
            PublishOrThrow(themeSettings);

            createdItems.Add("Theme Settings");
            CompleteStep(steps, "create-theme-settings", ["Created Theme Settings, imported primitive tokens, and published them before semantic role assignment."]);

            themeRoles = _contentService.Create("Styles", siteSettings, "settingsStyles");
            var themeRoleWarnings = ApplyThemeRoles(themeRoles, themeValidation.Theme);
            SaveOrThrow(themeRoles);
            createdItems.Add("Applied Theme Roles");
            warnings.AddRange(themeRoleWarnings);
            CompleteStep(steps, "create-site-theme-roles", themeRoleWarnings.Count == 0
                ? ["Created semantic theme role mappings."]
                : ["Created semantic theme role mappings.", .. themeRoleWarnings]);

            logoAssignment = await UploadLogosAsync(request.SiteName.Trim(), request.LogoFiles);
            warnings.AddRange(logoAssignment.Messages.Where(x => x.StartsWith("Warning:", StringComparison.Ordinal)));
            ApplyLogoAssignments(identity, header, footer, logoAssignment);
            SaveOrThrow(identity);
            SaveOrThrow(header);
            SaveOrThrow(footer);
            CompleteStep(steps, "upload-and-assign-logos", logoAssignment.Messages.Count == 0
                ? ["No logo files supplied."]
                : logoAssignment.Messages);

            CompleteStep(steps, "apply-domains-and-cultures", [
                "No domain input supplied. Domain assignment skipped.",
                "Content types are invariant. No culture assignment required."
            ]);

            CompleteStep(steps, "create-or-assign-users", ["No user-assignment input supplied. Step skipped."]);

            PublishOrThrow(home);
            PublishOrThrow(about);
            PublishOrThrow(identity);
            PublishOrThrow(header);
            PublishOrThrow(footer);
            PublishOrThrow(themeSettings);
            PublishOrThrow(themeRoles);
            PublishOrThrow(siteSettings);
            PublishOrThrow(root);
            CompleteStep(steps, "publish-required-nodes", ["Published tenant root, pages, and settings nodes."]);

            var stylesheetUrl = $"/css/generated-tokens.css?tenant={root.Key:D}";
            var generatedCss = _designTokenCssGenerator.GenerateCss(root.Key);
            WriteGeneratedStylesheet(tenantKey, generatedCss);

            manualActions.Add("Assign domains for the new tenant root before public preview.");
            manualActions.Add($"Add or update frontend integration to load '{stylesheetUrl}' if theme CSS is not already wired in.");

            CompleteStep(steps, "completion-summary", [
                $"Site '{request.SiteName.Trim()}' created.",
                $"Preview domain pending domain assignment for tenant key '{tenantKey}'.",
                $"Generated stylesheet endpoint: {stylesheetUrl}"
            ]);

            return BuildResponse(
                true,
                request.SiteName.Trim(),
                tenantKey,
                "Not assigned",
                steps,
                createdItems,
                userDetails,
                manualActions,
                warnings,
                stylesheetUrl);
        }
        catch (Exception ex)
        {
            var current = steps.FirstOrDefault(x => string.Equals(x.Status, "In progress", StringComparison.OrdinalIgnoreCase))
                ?? steps.FirstOrDefault(x => string.Equals(x.Status, "Not started", StringComparison.OrdinalIgnoreCase));

            if (current is not null)
            {
                FailStep(steps, current.Alias, [ex.Message]);
            }

            return BuildResponse(false, request.SiteName.Trim(), tenantKey, null, steps, createdItems, userDetails, manualActions, warnings);
        }
    }

    private static IReadOnlyList<MutableStep> CreateSteps() =>
    [
        new("validate-inputs", "Validate Inputs"),
        new("create-root-site", "Create Root Site"),
        new("create-home-page", "Create Home Page"),
        new("create-about-page", "Create About Page"),
        new("create-site-settings", "Create Site Settings"),
        new("create-identity-settings", "Create Identity Settings"),
        new("create-header-settings", "Create Header Settings"),
        new("create-footer-settings", "Create Footer Settings"),
        new("create-theme-settings", "Create Theme Settings"),
        new("create-site-theme-roles", "Create Site Theme Roles"),
        new("upload-and-assign-logos", "Upload And Assign Logos"),
        new("apply-domains-and-cultures", "Apply Domains And Cultures"),
        new("create-or-assign-users", "Create Or Assign Users"),
        new("publish-required-nodes", "Publish Required Nodes"),
        new("completion-summary", "Completion Summary")
    ];

    private static void CompleteStep(IReadOnlyList<MutableStep> steps, string alias, IEnumerable<string> messages)
    {
        var step = steps.First(x => x.Alias == alias);
        step.Status = "Complete";
        step.Messages.Clear();
        step.Messages.AddRange(messages);
        step.Errors.Clear();
    }

    private static void FailStep(IReadOnlyList<MutableStep> steps, string alias, IEnumerable<string> errors)
    {
        var step = steps.First(x => x.Alias == alias);
        step.Status = "Error";
        step.Errors.Clear();
        step.Errors.AddRange(errors);
    }

    private static SiteGenerationResponse BuildResponse(
        bool success,
        string? siteName,
        string? tenantKey,
        string? previewDomain,
        IReadOnlyList<MutableStep> steps,
        IReadOnlyList<string> createdItems,
        IReadOnlyList<string> userDetails,
        IReadOnlyList<string> manualActions,
        IReadOnlyList<string> warnings,
        string? stylesheetUrl = null)
    {
        return new SiteGenerationResponse(
            success,
            siteName,
            tenantKey,
            previewDomain,
            stylesheetUrl,
            steps.Select(x => x.ToResult()).ToArray(),
            createdItems,
            userDetails,
            manualActions,
            warnings);
    }

    private static List<string> ValidateSiteName(string? siteName)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(siteName))
        {
            errors.Add("Site name is required.");
            return errors;
        }

        if (siteName.Trim().Length < 2)
        {
            errors.Add("Site name must be at least 2 characters.");
        }

        return errors;
    }

    private static string CreateTenantKey(string siteName)
    {
        var builder = new StringBuilder();
        var trimmed = siteName.Trim().ToLowerInvariant();
        var pendingDash = false;

        foreach (var character in trimmed)
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                pendingDash = false;
                continue;
            }

            pendingDash = builder.Length > 0;
        }

        return builder.Length == 0 ? "site" : builder.ToString();
    }

    private static void ApplyThemeSettings(IContent content, SiteThemeImport theme)
    {
        content.SetValue("color", CreateBlockListJson(theme.Colors.Values
            .OrderBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
            .Select(color => new BlockPayload(
                new Guid("ab9f6723-0dbb-4a0c-8c2f-7bc655f12910"),
                [new PropertyPayload("label", color.Alias), new PropertyPayload("customValue", color.Value)]))));

        content.SetValue("spaceXs", CreateResponsiveJson(theme.CoreSpacing["xs"]));
        content.SetValue("spaceSm", CreateResponsiveJson(theme.CoreSpacing["sm"]));
        content.SetValue("spaceMd", CreateResponsiveJson(theme.CoreSpacing["md"]));
        content.SetValue("spaceLg", CreateResponsiveJson(theme.CoreSpacing["lg"]));
        content.SetValue("spaceXl", CreateResponsiveJson(theme.CoreSpacing["xl"]));
        content.SetValue("space2xl", CreateResponsiveJson(theme.CoreSpacing["2xl"]));
        content.SetValue("space", CreateBlockListJson(theme.AdditionalSpacing.Select(item => new BlockPayload(
            new Guid("d81e1092-3ab0-4f0b-9b72-a568d8bfb932"),
            [new PropertyPayload("name", item.Name), new PropertyPayload("responsiveValues", CreateResponsiveJson(item.Value))]))));

        content.SetValue("fontFamilySans", CreateFontFamilyJson(theme.SansFont));
        content.SetValue("fontFamilyDisplay", CreateFontFamilyJson(theme.DisplayFont));

        content.SetValue("fontSizeSm", CreateResponsiveJson(theme.FontSizes["sm"]));
        content.SetValue("fontSizeBase", CreateResponsiveJson(theme.FontSizes["base"]));
        content.SetValue("fontSizeLg", CreateResponsiveJson(theme.FontSizes["lg"]));
        content.SetValue("fontSizeXl", CreateResponsiveJson(theme.FontSizes["xl"]));
        content.SetValue("fontSize2xl", CreateResponsiveJson(theme.FontSizes["2xl"]));

        content.SetValue("lineHeightTight", CreateDimensionJson(theme.LineHeights["tight"]));
        content.SetValue("lineHeightBase", CreateDimensionJson(theme.LineHeights["base"]));
        content.SetValue("layoutGutter", CreateResponsiveJson(theme.LayoutGutter));
        content.SetValue("layoutWidthContent", CreateDimensionJson(theme.LayoutWidths["content"]));
        content.SetValue("layoutWidthReading", CreateDimensionJson(theme.LayoutWidths["reading"]));
        content.SetValue("radiusSm", CreateDimensionJson(theme.Radius["sm"]));
        content.SetValue("radiusMd", CreateDimensionJson(theme.Radius["md"]));
        content.SetValue("radiusLg", CreateDimensionJson(theme.Radius["lg"]));
        content.SetValue("radiusFull", CreateDimensionJson(theme.Radius["full"]));
        content.SetValue("shadowNone", theme.Shadow["none"]);
        content.SetValue("shadowMd", theme.Shadow["md"]);
        content.SetValue("shadowXl", theme.Shadow["xl"]);
    }

    private List<string> ApplyThemeRoles(IContent content, SiteThemeImport theme)
    {
        var warnings = new List<string>();
        var definedAliases = GetDefinedPropertyAliases(content.ContentType.Alias);

        foreach (var property in theme.SemanticColorAssignments)
        {
            TrySetThemeRoleValue(content, property.Key, property.Value, definedAliases, warnings);
        }

        foreach (var property in theme.SemanticValueAssignments)
        {
            TrySetThemeRoleValue(content, property.Key, property.Value, definedAliases, warnings);
        }

        foreach (var property in theme.DirectValues)
        {
            TrySetThemeRoleValue(content, property.Key, CreateDimensionJson(property.Value), definedAliases, warnings);
        }

        if (theme.AdditionalValueTokens.Count != 0)
        {
            TrySetThemeRoleValue(content, "additionalTokens", CreateBlockListJson(theme.AdditionalValueTokens.Select(item => new BlockPayload(
                new Guid("4317e2c1-58d1-442b-a6f4-a58b04782165"),
                [
                    new PropertyPayload("alias", item.Alias),
                    new PropertyPayload("label", item.Label),
                    new PropertyPayload("value", item.Value)
                ]))), definedAliases, warnings);
        }

        return warnings;
    }

    private HashSet<string> GetDefinedPropertyAliases(string contentTypeAlias)
    {
        var contentType = _contentTypeService.Get(contentTypeAlias);
        return contentType is null
            ? []
            : contentType.PropertyTypes
                .Select(x => x.Alias)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolvePropertyAlias(IReadOnlySet<string> definedAliases, string alias)
    {
        if (definedAliases.Contains(alias))
        {
            return alias;
        }

        var camelCaseAlias = ToCamelCaseAlias(alias);
        return definedAliases.Contains(camelCaseAlias)
            ? camelCaseAlias
            : string.Empty;
    }

    private static string ToCamelCaseAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias) || !alias.Contains('-'))
        {
            return alias;
        }

        var segments = alias.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return alias;
        }

        var builder = new StringBuilder(segments[0]);
        for (var index = 1; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (segment.Length == 0)
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(segment[0]));
            if (segment.Length > 1)
            {
                builder.Append(segment.AsSpan(1));
            }
        }

        return builder.ToString();
    }

    private static void TrySetThemeRoleValue(
        IContent content,
        string alias,
        object? value,
        IReadOnlySet<string> definedAliases,
        List<string> warnings)
    {
        var resolvedAlias = ResolvePropertyAlias(definedAliases, alias);
        if (string.IsNullOrWhiteSpace(resolvedAlias))
        {
            warnings.Add($"Skipped theme role '{alias}' because the current '{content.ContentType.Alias}' content type does not define that property.");
            return;
        }

        try
        {
            content.SetValue(resolvedAlias, value);
        }
        catch (Exception ex)
        {
            warnings.Add($"Skipped theme role '{alias}' because assignment failed: {ex.Message}");
        }
    }

    private async Task<LogoAssignmentResult> UploadLogosAsync(string siteName, IReadOnlyList<UploadedLogoAsset> files)
    {
        if (files.Count == 0)
        {
            return new LogoAssignmentResult(null, null, null, null, []);
        }

        var messages = new List<string>();
        var folder = _mediaService.CreateMedia(siteName, -1, "folder");
        _mediaService.Save(folder);
        messages.Add($"Created media folder '{siteName}'.");

        var uploaded = new List<IMedia>();

        foreach (var file in files)
        {
            var mediaTypeAlias = ResolveMediaTypeAlias(file.FileName, file.ContentType);
            var media = _mediaService.CreateMedia(Path.GetFileNameWithoutExtension(file.FileName), folder, mediaTypeAlias);
            using var stream = new MemoryStream(file.Bytes);
            media.SetValue(
                _mediaFileManager,
                _mediaUrlGeneratorCollection,
                _shortStringHelper,
                _contentTypeBaseServiceProvider,
                Constants.Conventions.Media.File,
                file.FileName,
                stream);
            _mediaService.Save(media);
            uploaded.Add(media);
            messages.Add($"Uploaded '{file.FileName}'.");
        }

        var headerLogo = uploaded.FirstOrDefault();
        var footerLogo = uploaded.Skip(1).FirstOrDefault() ?? headerLogo;
        var favicon = uploaded.FirstOrDefault(x => IsFaviconCandidate(x.Name)) ?? headerLogo;

        if (favicon is null)
        {
            messages.Add("Warning: no favicon candidate found in uploaded logo assets.");
        }

        return new LogoAssignmentResult(folder.Key, headerLogo?.Key, footerLogo?.Key, favicon?.Key, messages);
    }

    private static void ApplyLogoAssignments(IContent identity, IContent header, IContent footer, LogoAssignmentResult logos)
    {
        if (logos.FaviconMediaKey is Guid faviconKey)
        {
            identity.SetValue("favicon", CreateMediaPickerJson(faviconKey));
        }

        if (logos.HeaderLogoMediaKey is Guid headerLogoKey)
        {
            header.SetValue("header", CreateSingleBlockJson(new BlockPayload(
                new Guid("159513df-133d-49e7-9bcc-e110aa764558"),
                [new PropertyPayload("logo", CreateSingleBlockJson(new BlockPayload(
                    new Guid("f5ccb228-b3cb-46f3-9407-c37d5f221839"),
                    [new PropertyPayload("logo", CreateMediaPickerJson(headerLogoKey))])))])));
        }

        if (logos.FooterLogoMediaKey is Guid footerLogoKey)
        {
            footer.SetValue("footer", CreateSingleBlockJson(new BlockPayload(
                new Guid("c56b33e6-732a-4f80-a08a-eb618ebd7711"),
                [new PropertyPayload("footerLogo", CreateSingleBlockJson(new BlockPayload(
                    new Guid("f5ccb228-b3cb-46f3-9407-c37d5f221839"),
                    [new PropertyPayload("logo", CreateMediaPickerJson(footerLogoKey))])))])));
        }
    }

    private void WriteGeneratedStylesheet(string tenantKey, string css)
    {
        var directory = Path.Combine(Path.GetTempPath(), "site-generated-css");
        Directory.CreateDirectory(directory);
        System.IO.File.WriteAllText(Path.Combine(directory, $"{tenantKey}.tokens.css"), css, Encoding.UTF8);
    }

    private static bool IsFaviconCandidate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Contains("favicon", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("icon", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveMediaTypeAlias(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
        {
            return "umbracoMediaVectorGraphics";
        }

        if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        if (ContentTypeProvider.TryGetContentType(fileName, out var detected) &&
            detected.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        return "file";
    }

    private static string CreateFontFamilyJson(FontFamilyToken font) => JsonSerializer.Serialize(new
    {
        preset = font.Preset,
        customValue = font.Preset.Equals("custom", StringComparison.OrdinalIgnoreCase) ? font.CustomValue : string.Empty
    });

    private static string CreateResponsiveJson(ResponsiveTokenValue value) => JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        ["$type"] = "dimension",
        ["$value"] = new Dictionary<string, string>
        {
            ["mobile"] = value.Mobile,
            ["tablet"] = value.Tablet,
            ["desktop"] = value.Desktop
        }
    });

    private static string CreateDimensionJson(string value) => JsonSerializer.Serialize(new Dictionary<string, object?>
    {
        ["$type"] = "dimension",
        ["$value"] = value
    });

    private static string CreateMediaPickerJson(Guid mediaKey)
    {
        return JsonSerializer.Serialize(new[]
        {
            new
            {
                key = Guid.NewGuid(),
                mediaKey,
                mediaTypeAlias = "Image",
                crops = Array.Empty<object>(),
                focalPoint = (object?)null
            }
        });
    }

    private static string CreateSingleBlockJson(BlockPayload payload)
    {
        var key = Guid.NewGuid();
        var document = new
        {
            contentData = new[]
            {
                new
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
                }
            },
            settingsData = Array.Empty<object>(),
            expose = new[]
            {
                new
                {
                    contentKey = key,
                    culture = (string?)null,
                    segment = (string?)null
                }
            },
            Layout = new Dictionary<string, object>
            {
                ["Umbraco.SingleBlock"] = new[]
                {
                    new
                    {
                        contentUdi = (string?)null,
                        settingsUdi = (string?)null,
                        contentKey = key,
                        settingsKey = (Guid?)null
                    }
                }
            }
        };

        return JsonSerializer.Serialize(document);
    }

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

        var document = new
        {
            contentData,
            settingsData = Array.Empty<object>(),
            expose,
            Layout = new Dictionary<string, object>
            {
                ["Umbraco.BlockList"] = layout
            }
        };

        return JsonSerializer.Serialize(document);
    }

    private void SaveOrThrow(IContent content)
    {
        _contentService.Save(content);
    }

    private void PublishOrThrow(IContent content)
    {
        _contentService.Save(content);
        var result = _contentService.Publish(content, []);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to publish '{content.Name}'.");
        }
    }

    private sealed record BlockPayload(Guid ContentTypeKey, IReadOnlyList<PropertyPayload> Values);

    private sealed record PropertyPayload(string Alias, object? Value);

    private sealed class MutableStep
    {
        public MutableStep(string alias, string name)
        {
            Alias = alias;
            Name = name;
        }

        public string Alias { get; }
        public string Name { get; }
        public string Status { get; set; } = "Not started";
        public List<string> Messages { get; } = [];
        public List<string> Errors { get; } = [];

        public SiteGenerationStepResult ToResult() => new(Alias, Name, Status, Messages.ToArray(), Errors.ToArray());
    }
}

internal static class SubsidiarySiteGeneratorJsonExtensions
{
    public static JsonObject ToJsonObject<TValue>(this IReadOnlyDictionary<string, TValue> source, Func<TValue, JsonNode?> projector)
    {
        var result = new JsonObject();
        foreach (var item in source)
        {
            result[item.Key] = projector(item.Value);
        }

        return result;
    }
}
