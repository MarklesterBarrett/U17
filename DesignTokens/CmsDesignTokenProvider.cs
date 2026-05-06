using System.Text.Json;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Site.DesignTokens;

public sealed class CmsDesignTokenProvider : IDesignTokenProvider
{
    private const string StyleSettingsAlias = "styleSettings";
    private const string BaseColorsAlias = "baseColors";
    private const string SemanticColorsAlias = "semanticColors";
    private const string SpacingTokensAlias = "spacingTokens";
    private const string BaseColorAliasProperty = "baseColorAlias";
    private static readonly IReadOnlyList<(string PropertyAlias, string TokenAlias, string Label)> CoreSpacingProperties =
    [
        ("spacingXs", "space-xs", "Tight"),
        ("spacingSm", "space-sm", "Compact"),
        ("spacingMd", "space-md", "Comfortable"),
        ("spacingLg", "space-lg", "Spacious"),
        ("spacingXl", "space-xl", "Large"),
        ("spacing2Xl", "space-2xl", "Extra large")
    ];
    private static readonly IReadOnlyList<(string PropertyAlias, string TokenAlias, string Label)> FixedColorProperties =
    [
        ("brand", "brand", "Brand"),
        ("brandHover", "brand-hover", "Brand Hover"),
        ("surfacePage", "surface-page", "Page"),
        ("surfacePanel", "surface-panel", "Panel"),
        ("surfacePanelMuted", "surface-panel-muted", "Panel Muted"),
        ("surfaceHeader", "surface-header", "Header"),
        ("surfaceFooter", "surface-footer", "Footer"),
        ("textDefault", "text-default", "Default Text"),
        ("textStrong", "text-strong", "Strong Text"),
        ("textMuted", "text-muted", "Muted Text"),
        ("textInverse", "text-inverse", "Inverse Text"),
        ("borderSubtle", "border-subtle", "Subtle Border"),
        ("borderStrong", "border-strong", "Strong Border"),
        ("actionPrimaryBg", "action-primary-bg", "Primary Action Background"),
        ("actionPrimaryBgHover", "action-primary-bg-hover", "Primary Action Background Hover"),
        ("actionPrimaryText", "action-primary-text", "Primary Action Text"),
        ("actionAccent", "action-accent", "Accent"),
        ("focusRing", "focus-ring-color", "Focus Ring")
    ];
    private static readonly IReadOnlyList<(string PropertyAlias, string TokenAlias, string Label)> FixedValueProperties =
    [
        ("radiusSm", "radius-sm", "Subtle"),
        ("radiusMd", "radius-md", "Standard"),
        ("radiusLg", "radius-lg", "Prominent"),
        ("radiusFull", "radius-full", "Full"),
        ("fontFamilySans", "font-family-sans", "Default Font"),
        ("fontFamilyDisplay", "font-family-display", "Feature Font"),
        ("fontSizeSm", "font-size-sm", "Small"),
        ("fontSizeBase", "font-size-base", "Standard"),
        ("fontSizeLg", "font-size-lg", "Large"),
        ("fontSizeXl", "font-size-xl", "Heading"),
        ("fontSize2Xl", "font-size-2xl", "Display"),
        ("lineHeightTight", "line-height-tight", "Tight Line Height"),
        ("lineHeightBase", "line-height-base", "Base Line Height"),
        ("layoutGutter", "layout-gutter", "Gutter"),
        ("layoutWidthContent", "layout-width-content", "Content Width"),
        ("layoutWidthReading", "layout-width-reading", "Reading Width"),
        ("shadowNone", "shadow-none", "Flat"),
        ("shadowMd", "shadow-md", "Raised"),
        ("shadowXl", "shadow-xl", "Lifted")
    ];
    private static readonly HashSet<string> TypographyPropertyAliases =
    [
        "fontFamilySans",
        "fontFamilyDisplay",
        "fontSizeSm",
        "fontSizeBase",
        "fontSizeLg",
        "fontSizeXl",
        "fontSize2Xl",
        "lineHeightTight",
        "lineHeightBase"
    ];
    private static readonly HashSet<string> ThemeTopLevelValueAliases =
    [
        "layoutGutter",
        "layoutWidthContent",
        "layoutWidthReading",
        "radiusSm",
        "radiusMd",
        "radiusLg",
        "radiusFull",
        "shadowNone",
        "shadowMd",
        "shadowXl"
    ];
    private static readonly IReadOnlyDictionary<string, string> FontFamilyPresetValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sans"] = "ui-sans-serif, system-ui, sans-serif",
            ["serif"] = "ui-serif, Georgia, Cambria, \"Times New Roman\", Times, serif",
            ["mono"] = "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace"
        };
    private readonly ISiteSettingsResolver _siteSettingsResolver;

    public CmsDesignTokenProvider(ISiteSettingsResolver siteSettingsResolver)
    {
        _siteSettingsResolver = siteSettingsResolver;
    }

    public DesignTokenSet GetTokens()
    {
        var siteSettings = _siteSettingsResolver.GetSiteSettings();
        var themeSettings = _siteSettingsResolver.GetThemeSettings();
        var designTokens = GetDesignTokens(themeSettings, siteSettings);

        if (designTokens is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(themeSettings, siteSettings, designTokens),
            GetSpacingTokens(themeSettings, siteSettings),
            GetValueTokens(themeSettings, siteSettings, designTokens));
    }

    public DesignTokenSet GetTokens(Guid tenantKey)
    {
        var siteSettings = _siteSettingsResolver.GetSiteSettings(tenantKey);
        var themeSettings = _siteSettingsResolver.GetThemeSettings(tenantKey);
        var designTokens = GetDesignTokens(themeSettings, siteSettings);

        if (designTokens is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(themeSettings, siteSettings, designTokens),
            GetSpacingTokens(themeSettings, siteSettings),
            GetValueTokens(themeSettings, siteSettings, designTokens));
    }

    private static IPublishedElement? GetDesignTokens(IPublishedContent? themeSettings, IPublishedContent? siteSettings)
    {
        return themeSettings
            ?.Value<BlockListItem>(StyleSettingsAlias)
            ?.Content
            ?? siteSettings
            ?.Value<BlockListItem>(StyleSettingsAlias)
            ?.Content;
    }

    private static IReadOnlyList<ColorTokenDefinition> GetColorTokens(
        IPublishedContent? themeSettings,
        IPublishedContent? siteSettings,
        IPublishedElement designTokens)
    {
        var primitiveColors = GetPrimitiveColors(themeSettings, siteSettings, designTokens);
        var semanticColors = GetSemanticColorTokens(designTokens, primitiveColors);

        if (semanticColors.Count != 0)
        {
            return semanticColors;
        }

        return FixedColorProperties
            .Select(definition => new ColorTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                ResolveFixedSemanticColorValue(designTokens, primitiveColors, definition.PropertyAlias)))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value))
            .ToList();
    }

    private static IReadOnlyDictionary<string, string> GetPrimitiveColors(
        IPublishedContent? themeSettings,
        IPublishedContent? siteSettings,
        IPublishedElement designTokens)
    {
        var primitiveColorBlocks = themeSettings?.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = siteSettings?.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);
        }

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = designTokens.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);
        }

        return (primitiveColorBlocks ?? Enumerable.Empty<BlockListItem>())
            .Select(x => x.Content)
            .Where(x => x is not null)
            .Select(x => new
            {
                Alias = ResolvePrimitiveColorAlias(x!),
                Value = ResolvePrimitiveColorValue(x)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Alias) &&
                        !string.IsNullOrWhiteSpace(x.Value))
            .GroupBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<SpacingTokenDefinition> GetSpacingTokens(IPublishedContent? themeSettings, IPublishedContent? siteSettings)
    {
        var spacingContainer = themeSettings ?? siteSettings;

        if (spacingContainer is null)
        {
            return EnsureZeroSpacingToken([]);
        }

        var coreTokens = CoreSpacingProperties
            .Select(definition => CreateCoreSpacingToken(spacingContainer, definition.PropertyAlias, definition.TokenAlias, definition.Label))
            .Where(token => token is not null)
            .Select(token => token!)
            .ToList();

        var additionalTokens = (spacingContainer.Value<IEnumerable<BlockListItem>>(SpacingTokensAlias) ?? Enumerable.Empty<BlockListItem>())
            .Select(block => block.Content)
            .Where(content => content is not null)
            .Select(content => CreateAdditionalSpacingToken(content!))
            .Where(x => !string.IsNullOrWhiteSpace(x.Alias) &&
                        !string.IsNullOrWhiteSpace(x.Label) &&
                        !string.IsNullOrWhiteSpace(x.Mobile) &&
                        !string.IsNullOrWhiteSpace(x.Tablet) &&
                        !string.IsNullOrWhiteSpace(x.Desktop))
            .ToList();

        var tokens = coreTokens
            .Concat(additionalTokens)
            .GroupBy(token => token.Alias, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        return EnsureZeroSpacingToken(tokens);
    }

    private static IReadOnlyList<ValueTokenDefinition> GetValueTokens(
        IPublishedContent? themeSettings,
        IPublishedContent? siteSettings,
        IPublishedElement designTokens)
    {
        var builtInTokens = new[]
        {
            new ValueTokenDefinition("radius-none", "None", "0")
        };

        var typographyContainer = themeSettings ?? siteSettings;

        var fixedTokens = FixedValueProperties
            .Select(definition => CreateValueTokenDefinition(typographyContainer, designTokens, definition))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value));

        var additionalTokens = (designTokens.Value<IEnumerable<BlockListItem>>("additionalTokens") ?? Enumerable.Empty<BlockListItem>())
            .Select(block => block.Content)
            .Where(content => content is not null)
            .Select(content => new ValueTokenDefinition(
                content!.Value<string>("alias")?.Trim() ?? string.Empty,
                content.Value<string>("label")?.Trim() ?? string.Empty,
                content.Value<string>("value")?.Trim() ?? string.Empty))
            .Where(token => !string.IsNullOrWhiteSpace(token.Alias) &&
                            !string.IsNullOrWhiteSpace(token.Label) &&
                            !string.IsNullOrWhiteSpace(token.Value));

        return builtInTokens
            .Concat(fixedTokens)
            .Concat(additionalTokens)
            .ToList();
    }

    private static IReadOnlyList<ColorTokenDefinition> GetSemanticColorTokens(
        IPublishedElement designTokens,
        IReadOnlyDictionary<string, string> primitiveColors)
    {
        return (designTokens.Value<IEnumerable<BlockListItem>>(SemanticColorsAlias) ?? Enumerable.Empty<BlockListItem>())
            .Select(x => x.Content)
            .Where(x => x is not null)
            .Select(x => new ColorTokenDefinition(
                x!.Value<string>("alias")?.Trim() ?? string.Empty,
                x.Value<string>("label")?.Trim() ?? string.Empty,
                ResolveSemanticColorValue(x, primitiveColors)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Alias) &&
                        !string.IsNullOrWhiteSpace(x.Label) &&
                        !string.IsNullOrWhiteSpace(x.Value))
            .ToList();
    }

    private static string ResolvePrimitiveColorValue(IPublishedElement token)
    {
        var customValue = token.Value<string>("customValue")?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(customValue))
        {
            return customValue;
        }

        var paletteAlias = token.Value<string>("paletteValue")?.Trim() ?? string.Empty;
        return Contentment.BuiltInBaseColorDataSource.TryGetColorValue(paletteAlias, out var paletteValue)
            ? paletteValue
            : string.Empty;
    }

    private static string ResolvePrimitiveColorAlias(IPublishedElement token)
    {
        var paletteAlias = token.Value<string>("paletteValue")?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(paletteAlias))
        {
            return paletteAlias;
        }

        return token.Value<string>("label")?.Trim() ?? string.Empty;
    }

    private static string ResolveSemanticColorValue(IPublishedElement token, IReadOnlyDictionary<string, string> primitiveColors)
    {
        var primitiveAlias = token.Value<string>(BaseColorAliasProperty)?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(primitiveAlias) &&
            primitiveColors.TryGetValue(primitiveAlias, out var primitiveValue) &&
            !string.IsNullOrWhiteSpace(primitiveValue))
        {
            return primitiveValue;
        }

        return string.Empty;
    }

    private static string ResolveFixedSemanticColorValue(
        IPublishedElement designTokens,
        IReadOnlyDictionary<string, string> primitiveColors,
        string propertyAlias)
    {
        var selectedPrimitiveAlias = designTokens.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(selectedPrimitiveAlias) &&
            primitiveColors.TryGetValue(selectedPrimitiveAlias, out var primitiveValue) &&
            !string.IsNullOrWhiteSpace(primitiveValue))
        {
            return primitiveValue;
        }

        return selectedPrimitiveAlias;
    }


    private static SpacingTokenDefinition CreateAdditionalSpacingToken(IPublishedElement content)
    {
        var name = content.Value<string>("name")?.Trim()
            ?? content.Value<string>("label")?.Trim()
            ?? string.Empty;
        var value = GetResponsiveSpacingValue(content, "responsiveValues");

        return CreateNamedSpacingToken(name, value.Mobile, value.Tablet, value.Desktop);
    }

    private static SpacingTokenDefinition? CreateCoreSpacingToken(
        IPublishedElement designTokens,
        string propertyAlias,
        string tokenAlias,
        string label)
    {
        var value = GetResponsiveSpacingValue(designTokens, propertyAlias);
        var mobile = value.Mobile;
        var tablet = value.Tablet;
        var desktop = value.Desktop;

        if (string.IsNullOrWhiteSpace(mobile) ||
            string.IsNullOrWhiteSpace(tablet) ||
            string.IsNullOrWhiteSpace(desktop))
        {
            return null;
        }

        return CreateSpacingToken(tokenAlias, label, mobile, tablet, desktop);
    }

    private static ResponsiveSpacingValue GetResponsiveSpacingValue(IPublishedElement source, string propertyAlias)
    {
        var jsonValue = source.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

        return !string.IsNullOrWhiteSpace(jsonValue) &&
               TryParseResponsiveSpacingValue(jsonValue, out var parsedValue)
            ? parsedValue
            : ResponsiveSpacingValue.Empty;
    }

    private static bool TryParseResponsiveSpacingValue(string jsonValue, out ResponsiveSpacingValue value)
    {
        value = ResponsiveSpacingValue.Empty;

        try
        {
            using var document = JsonDocument.Parse(jsonValue);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            value = new ResponsiveSpacingValue(
                GetJsonString(root, "mobile"),
                GetJsonString(root, "tablet"),
                GetJsonString(root, "desktop"));

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string GetJsonString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static SpacingTokenDefinition CreateNamedSpacingToken(
        string name,
        string mobile,
        string tablet,
        string desktop)
    {
        var alias = CreateSpacingAlias(name);
        return CreateSpacingToken(alias, name, mobile, tablet, desktop);
    }

    private static string CreateSpacingAlias(string name)
    {
        var normalizedName = name.Trim();

        if (IsZeroSpacingAlias(normalizedName))
        {
            return "space-none";
        }

        if (normalizedName.StartsWith("space-", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedName.ToLowerInvariant();
        }

        var slug = string.Join(
            "-",
            normalizedName
                .ToLowerInvariant()
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(slug)
            ? string.Empty
            : $"space-{slug}";
    }

    private static SpacingTokenDefinition CreateSpacingToken(
        string alias,
        string label,
        string mobile,
        string tablet,
        string desktop)
    {
        if (IsZeroSpacingAlias(alias))
        {
            return new SpacingTokenDefinition("space-none", string.IsNullOrWhiteSpace(label) ? "None" : label, "0", "0", "0");
        }

        return new SpacingTokenDefinition(alias, label, mobile, tablet, desktop);
    }

    private static IReadOnlyList<SpacingTokenDefinition> EnsureZeroSpacingToken(IReadOnlyList<SpacingTokenDefinition> tokens)
    {
        var nonZeroTokens = tokens
            .Where(x => !IsZeroSpacingAlias(x.Alias))
            .ToList();

        return new[]
            {
                new SpacingTokenDefinition("space-none", "None", "0", "0", "0")
            }
            .Concat(nonZeroTokens)
            .ToList();
    }

    private static bool IsZeroSpacingAlias(string? alias)
    {
        return string.Equals(alias, "0", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(alias, "none", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(alias, "space-none", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct ResponsiveSpacingValue(string Mobile, string Tablet, string Desktop)
    {
        public static readonly ResponsiveSpacingValue Empty = new(string.Empty, string.Empty, string.Empty);
    }


    private static string ResolveFixedValueTokenValue(
        IPublishedElement? themeSettings,
        IPublishedElement designTokens,
        string propertyAlias)
    {
        if (TypographyPropertyAliases.Contains(propertyAlias) ||
            ThemeTopLevelValueAliases.Contains(propertyAlias))
        {
            var themeValue = themeSettings?.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

            if (IsFontFamilyProperty(propertyAlias))
            {
                themeValue = ResolveFontFamilyValue(themeValue);
            }

            if (!string.IsNullOrWhiteSpace(themeValue))
            {
                return themeValue;
            }
        }

        var legacyValue = designTokens.Value<string>(propertyAlias)?.Trim() ?? string.Empty;
        return IsFontFamilyProperty(propertyAlias)
            ? ResolveFontFamilyValue(legacyValue)
            : legacyValue;
    }

    private static ValueTokenDefinition CreateValueTokenDefinition(
        IPublishedElement? themeSettings,
        IPublishedElement designTokens,
        (string PropertyAlias, string TokenAlias, string Label) definition)
    {
        if (IsResponsiveValueProperty(definition.PropertyAlias))
        {
            var value = GetResponsiveValue(themeSettings, designTokens, definition.PropertyAlias);

            return new ValueTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                value.Mobile,
                value.Tablet,
                value.Desktop);
        }

        return new ValueTokenDefinition(
            definition.TokenAlias,
            definition.Label,
            ResolveFixedValueTokenValue(themeSettings, designTokens, definition.PropertyAlias));
    }

    private static bool IsResponsiveValueProperty(string propertyAlias)
    {
        return string.Equals(propertyAlias, "fontSizeSm", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "fontSizeBase", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "fontSizeLg", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "fontSizeXl", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "fontSize2Xl", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "layoutGutter", StringComparison.Ordinal);
    }

    private static bool IsFontFamilyProperty(string propertyAlias)
    {
        return string.Equals(propertyAlias, "fontFamilySans", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "fontFamilyDisplay", StringComparison.Ordinal);
    }

    private static ResponsiveSpacingValue GetResponsiveValue(
        IPublishedElement? themeSettings,
        IPublishedElement designTokens,
        string propertyAlias)
    {
        var themeJson = themeSettings?.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(themeJson) &&
            TryParseResponsiveSpacingValue(themeJson, out var themeValue))
        {
            return themeValue;
        }

        var legacyValue = designTokens.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(legacyValue))
        {
            return new ResponsiveSpacingValue(legacyValue, legacyValue, legacyValue);
        }

        return ResponsiveSpacingValue.Empty;
    }

    private static string ResolveFontFamilyValue(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(rawValue);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return rawValue.Trim();
            }

            var preset = GetJsonString(root, "preset");
            var customValue = GetJsonString(root, "customValue");

            if (string.Equals(preset, "custom", StringComparison.OrdinalIgnoreCase))
            {
                return customValue;
            }

            return FontFamilyPresetValues.TryGetValue(preset, out var presetValue)
                ? presetValue
                : rawValue.Trim();
        }
        catch (JsonException)
        {
            return rawValue.Trim();
        }
    }
}
