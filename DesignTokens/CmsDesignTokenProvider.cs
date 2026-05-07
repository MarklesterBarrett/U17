using System.Text.Json;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Site.DesignTokens;

public sealed class CmsDesignTokenProvider : IDesignTokenProvider
{
    private const string BaseColorsAlias = "color";
    private const string SemanticColorsAlias = "semanticColors";
    private const string SpacingTokensAlias = "space";
    private const string AdditionalTokensAlias = "additional-tokens";
    private const string BaseColorAliasProperty = "baseColorAlias";
    private static readonly IReadOnlyList<ResponsivePropertyDefinition> CoreSpacingProperties =
    [
        new("space-xs", "space-xs", "Tight"),
        new("space-sm", "space-sm", "Compact"),
        new("space-md", "space-md", "Comfortable"),
        new("space-lg", "space-lg", "Spacious"),
        new("space-xl", "space-xl", "Large"),
        new("space-2xl", "space-2xl", "Extra large")
    ];
    private static readonly IReadOnlyList<SemanticColorPropertyDefinition> FixedColorProperties =
    [
        new("link-color", "brand", "Brand"),
        new("link-hover-color", "brand-hover", "Brand Hover"),
        new("primary-button-background", "action-primary-bg", "Primary Button Background"),
        new("primary-button-hover-background", "action-primary-bg-hover", "Primary Button Hover Background"),
        new("primary-button-text", "action-primary-text", "Primary Button Text"),
        new("primary-button-hover-text", "action-primary-text-hover", "Primary Button Hover Text"),
        new("secondary-button-background", "action-secondary-bg", "Secondary Button Background"),
        new("secondary-button-hover-background", "action-secondary-bg-hover", "Secondary Button Hover Background"),
        new("secondary-button-text", "action-secondary-text", "Secondary Button Text"),
        new("secondary-button-hover-text", "action-secondary-text-hover", "Secondary Button Hover Text"),
        new("secondary-button-background", "action-accent", "Accent"),
        new("disabled-background", "action-disabled-bg", "Disabled Background"),
        new("disabled-text", "action-disabled-text", "Disabled Text"),
        new("disabled-border", "action-disabled-border", "Disabled Border"),
        new("link-color", "link-color", "Link Colour"),
        new("link-hover-color", "link-hover-color", "Link Hover Colour"),
        new("link-active-color", "link-active-color", "Link Active Colour"),
        new("link-visited-color", "link-visited-color", "Link Visited Colour"),
        new("link-focus-color", "link-focus-color", "Link Focus Colour"),
        new("focus-ring-color", "focus-ring-color", "Focus Ring Colour"),
        new("body-text-color", "text-default", "Body Text Colour"),
        new("heading-text-color", "text-strong", "Heading Text Colour"),
        new("muted-text-color", "text-muted", "Muted Text Colour"),
        new("inverted-text-color", "text-inverse", "Inverted Text Colour"),
        new("page-background", "surface-page", "Page Background"),
        new("section-background", "surface-section", "Section Background"),
        new("section-background", "surface-panel-muted", "Panel Muted Background"),
        new("card-background", "surface-card", "Card Background"),
        new("card-background", "surface-panel", "Panel Background"),
        new("inverted-background", "surface-inverse", "Inverted Background"),
        new("default-border-color", "border-default", "Default Border Colour"),
        new("subtle-border-color", "border-subtle", "Subtle Border Colour"),
        new("strong-border-color", "border-strong", "Strong Border Colour"),
        new("input-background", "input-background", "Input Background"),
        new("input-text", "input-text", "Input Text"),
        new("input-border", "input-border", "Input Border"),
        new("input-border-focus", "input-focus-border", "Input Border Focus"),
        new("input-placeholder", "input-placeholder", "Input Placeholder"),
        new("validation-error-color", "validation-error", "Validation Error Colour"),
        new("validation-success-color", "validation-success", "Validation Success Colour"),
        new("success-color", "feedback-success", "Success Colour"),
        new("warning-color", "feedback-warning", "Warning Colour"),
        new("error-color", "feedback-error", "Error Colour"),
        new("info-color", "feedback-info", "Info Colour"),
        new("header-background", "header-background", "Header Background"),
        new("header-text-color", "header-text", "Header Text Colour"),
        new("header-link-color", "header-link", "Header Link Colour"),
        new("header-link-hover-color", "header-link-hover", "Header Link Hover Colour"),
        new("footer-background", "footer-background", "Footer Background"),
        new("footer-text-color", "footer-text", "Footer Text Colour"),
        new("footer-link-color", "footer-link", "Footer Link Colour"),
        new("footer-link-hover-color", "footer-link-hover", "Footer Link Hover Colour")
    ];
    private static readonly IReadOnlyList<SemanticValuePropertyDefinition> SemanticValueProperties =
    [
        new("card-radius", "radius-card", "Card Radius"),
        new("card-shadow", "shadow-card", "Card Shadow"),
        new("section-spacing", "space-section", "Section Spacing"),
        new("container-width", "layout-width-container", "Container Width")
    ];
    private static readonly IReadOnlyList<DirectValuePropertyDefinition> DirectValueProperties =
    [
        new("focus-ring-width", "focus-ring-width", "Focus Ring Width"),
        new("focus-ring-offset", "focus-ring-offset", "Focus Ring Offset")
    ];
    private static readonly IReadOnlyList<FixedValuePropertyDefinition> FixedValueProperties =
    [
        new("radius-sm", "radius-sm", "Subtle"),
        new("radius-md", "radius-md", "Standard"),
        new("radius-lg", "radius-lg", "Prominent"),
        new("radius-full", "radius-full", "Full"),
        new("font-family-sans", "font-family-sans", "Default Font"),
        new("fontFamilyDisplay", "font-family-display", "Feature Font"),
        new("font-size-sm", "font-size-sm", "Small"),
        new("font-size-base", "font-size-base", "Standard"),
        new("font-size-lg", "font-size-lg", "Large"),
        new("font-size-xl", "font-size-xl", "Heading"),
        new("font-size-2xl", "font-size-2xl", "Display"),
        new("line-height-tight", "line-height-tight", "Tight Line Height"),
        new("line-height-base", "line-height-base", "Base Line Height"),
        new("layout-gutter", "layout-gutter", "Gutter"),
        new("layout-width-content", "layout-width-content", "Content Width"),
        new("layout-width-reading", "layout-width-reading", "Reading Width"),
        new("shadow-none", "shadow-none", "Flat"),
        new("shadow-md", "shadow-md", "Raised"),
        new("shadow-xl", "shadow-xl", "Lifted")
    ];
    private static readonly IReadOnlyDictionary<string, string> FontFamilyPresetValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sans"] = "ui-sans-serif, system-ui, sans-serif",
            ["serif"] = "ui-serif, Georgia, Cambria, \"Times New Roman\", Times, serif",
            ["mono"] = "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace"
        };
    private static readonly IReadOnlyDictionary<string, string> DefaultShadowValues =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["shadow-none"] = "0 0 #0000",
            ["shadow-md"] = "0 2px 8px rgb(0 0 0 / 0.08)",
            ["shadow-xl"] = "0 12px 32px rgb(0 0 0 / 0.16)"
        };
    private readonly ISiteSettingsResolver _siteSettingsResolver;

    public CmsDesignTokenProvider(ISiteSettingsResolver siteSettingsResolver)
    {
        _siteSettingsResolver = siteSettingsResolver;
    }

    public DesignTokenSet GetTokens()
    {
        var themeSettings = _siteSettingsResolver.GetThemeSettings();
        var designTokens = _siteSettingsResolver.GetSemanticThemeTokens();

        if (designTokens is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(themeSettings, designTokens),
            GetSpacingTokens(themeSettings),
            GetValueTokens(themeSettings, designTokens));
    }

    public DesignTokenSet GetTokens(Guid tenantKey)
    {
        var themeSettings = _siteSettingsResolver.GetThemeSettings(tenantKey);
        var designTokens = _siteSettingsResolver.GetSemanticThemeTokens(tenantKey);

        if (designTokens is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(themeSettings, designTokens),
            GetSpacingTokens(themeSettings),
            GetValueTokens(themeSettings, designTokens));
    }

    private static IReadOnlyList<ColorTokenDefinition> GetColorTokens(
        IPublishedContent? themeSettings,
        IPublishedContent designTokens)
    {
        var primitiveColors = GetPrimitiveColors(themeSettings);
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

    private static IReadOnlyDictionary<string, string> GetPrimitiveColors(IPublishedContent? themeSettings)
    {
        var primitiveColorBlocks = themeSettings?.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);
        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in (primitiveColorBlocks ?? Enumerable.Empty<BlockListItem>())
                     .Select(x => x.Content)
                     .Where(x => x is not null))
        {
            var value = ResolvePrimitiveColorValue(token!);

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var canonicalAlias = ResolvePrimitiveColorAlias(token!);
            var legacyAlias = ResolvePrimitiveColorLegacyAlias(token!);

            if (!string.IsNullOrWhiteSpace(canonicalAlias))
            {
                colors[canonicalAlias] = value;
            }

            if (!string.IsNullOrWhiteSpace(legacyAlias))
            {
                colors[legacyAlias] = value;
            }
        }

        return colors;
    }

    private static IReadOnlyList<SpacingTokenDefinition> GetSpacingTokens(IPublishedContent? themeSettings)
    {
        if (themeSettings is null)
        {
            return EnsureZeroSpacingToken([]);
        }

        var coreTokens = CoreSpacingProperties
            .Select(definition => CreateCoreSpacingToken(themeSettings, definition))
            .Where(token => token is not null)
            .Select(token => token!)
            .ToList();

        var additionalTokens = (themeSettings.Value<IEnumerable<BlockListItem>>(SpacingTokensAlias)
                ?? Enumerable.Empty<BlockListItem>())
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
        IPublishedContent designTokens)
    {
        var builtInTokens = new[]
        {
            new ValueTokenDefinition("radius-none", "None", "0")
        };

        var fixedTokens = FixedValueProperties
            .Select(definition => CreateValueTokenDefinition(themeSettings, definition))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value));

        var semanticValueTokens = SemanticValueProperties
            .Select(definition => new ValueTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                ResolveSemanticValueReference(designTokens, definition.PropertyAlias)))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value));

        var directValueTokens = DirectValueProperties
            .Select(definition => CreateDirectValueTokenDefinition(designTokens, definition))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value));

        var additionalTokens = (designTokens.Value<IEnumerable<BlockListItem>>(AdditionalTokensAlias) ?? Enumerable.Empty<BlockListItem>())
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
            .Concat(semanticValueTokens)
            .Concat(directValueTokens)
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
            return ColorTokenAlias.ToCanonical(paletteAlias);
        }

        return ColorTokenAlias.ToCanonical(token.Value<string>("label")?.Trim() ?? string.Empty);
    }

    private static string ResolvePrimitiveColorLegacyAlias(IPublishedElement token)
    {
        var paletteAlias = token.Value<string>("paletteValue")?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(paletteAlias))
        {
            return ColorTokenAlias.ToLegacy(paletteAlias);
        }

        return ColorTokenAlias.ToLegacy(token.Value<string>("label")?.Trim() ?? string.Empty);
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
        var selectedPrimitiveAlias = GetPropertyValue(designTokens, propertyAlias);

        return !string.IsNullOrWhiteSpace(selectedPrimitiveAlias) &&
               primitiveColors.TryGetValue(selectedPrimitiveAlias, out var primitiveValue) &&
               !string.IsNullOrWhiteSpace(primitiveValue)
            ? primitiveValue
            : string.Empty;
    }

    private static string ResolveSemanticValueReference(IPublishedElement designTokens, string propertyAlias)
    {
        var selectedTokenAlias = GetPropertyValue(designTokens, propertyAlias);

        if (string.IsNullOrWhiteSpace(selectedTokenAlias))
        {
            return string.Empty;
        }

        return selectedTokenAlias.StartsWith("var(", StringComparison.OrdinalIgnoreCase)
            ? selectedTokenAlias
            : $"var(--{DesignTokenCssName.FromAlias(selectedTokenAlias)})";
    }

    private static string GetPropertyValue(IPublishedElement source, string propertyAlias) =>
        source.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

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
        ResponsivePropertyDefinition definition)
    {
        var value = ResolveResponsiveValue(GetResponsiveSpacingValue(designTokens, definition.PropertyAlias));
        var mobile = value.Mobile;
        var tablet = value.Tablet;
        var desktop = value.Desktop;

        if (string.IsNullOrWhiteSpace(mobile) ||
            string.IsNullOrWhiteSpace(tablet) ||
            string.IsNullOrWhiteSpace(desktop))
        {
            return null;
        }

        return CreateSpacingToken(definition.TokenAlias, definition.Label, mobile, tablet, desktop);
    }

    private static ResponsiveSpacingValue GetResponsiveSpacingValue(IPublishedElement source, string propertyAlias)
    {
        var jsonValue = GetPropertyValue(source, propertyAlias);

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

            if (root.TryGetProperty("$value", out var wrappedValue) &&
                wrappedValue.ValueKind == JsonValueKind.Object)
            {
                root = wrappedValue;
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
        var value = ResolveResponsiveValue(new ResponsiveSpacingValue(mobile, tablet, desktop));
        return CreateSpacingToken(alias, name, value.Mobile, value.Tablet, value.Desktop);
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

        return new SpacingTokenDefinition(
            alias,
            label,
            NormalizePixelLength(mobile),
            NormalizePixelLength(tablet),
            NormalizePixelLength(desktop));
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
        string propertyAlias)
    {
        var themeValue = themeSettings?.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

        if (IsFontFamilyProperty(propertyAlias))
        {
            return ResolveFontFamilyValue(themeValue);
        }

        themeValue = ResolveDimensionValue(themeValue);

        if (!string.IsNullOrWhiteSpace(themeValue))
        {
            if (ShouldNormalizePixelLength(propertyAlias))
            {
                themeValue = NormalizePixelLength(themeValue);
            }

            return themeValue;
        }

        if (DefaultShadowValues.TryGetValue(propertyAlias, out var defaultShadowValue))
        {
            return defaultShadowValue;
        }

        return string.Empty;
    }

    private static ValueTokenDefinition CreateValueTokenDefinition(
        IPublishedElement? themeSettings,
        FixedValuePropertyDefinition definition)
    {
        if (IsResponsiveValueProperty(definition.PropertyAlias))
        {
            var value = GetResponsiveValue(themeSettings, definition.PropertyAlias);

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
            ResolveFixedValueTokenValue(themeSettings, definition.PropertyAlias));
    }

    private static bool IsResponsiveValueProperty(string propertyAlias)
    {
        return string.Equals(propertyAlias, "font-size-sm", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "font-size-base", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "font-size-lg", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "font-size-xl", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "font-size-2xl", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "layout-gutter", StringComparison.Ordinal);
    }

    private static bool IsFontFamilyProperty(string propertyAlias)
    {
        return string.Equals(propertyAlias, "font-family-sans", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "fontFamilyDisplay", StringComparison.Ordinal);
    }

    private static ResponsiveSpacingValue GetResponsiveValue(
        IPublishedElement? themeSettings,
        string propertyAlias)
    {
        var themeJson = themeSettings?.Value<string>(propertyAlias)?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(themeJson) &&
            TryParseResponsiveSpacingValue(themeJson, out var themeValue))
        {
            var resolvedValue = ResolveResponsiveValue(themeValue);
            return new ResponsiveSpacingValue(
                resolvedValue.Mobile,
                resolvedValue.Tablet,
                resolvedValue.Desktop);
        }

        return ResponsiveSpacingValue.Empty;
    }

    private static ResponsiveSpacingValue ResolveResponsiveValue(ResponsiveSpacingValue value)
    {
        if (string.IsNullOrWhiteSpace(value.Mobile) &&
            string.IsNullOrWhiteSpace(value.Tablet) &&
            string.IsNullOrWhiteSpace(value.Desktop))
        {
            return ResponsiveSpacingValue.Empty;
        }

        var mobile = value.Mobile.Trim();
        var tablet = value.Tablet.Trim();
        var desktop = value.Desktop.Trim();

        if (string.IsNullOrWhiteSpace(tablet))
        {
            tablet = mobile;
        }

        if (string.IsNullOrWhiteSpace(desktop))
        {
            desktop = tablet;
        }

        return new ResponsiveSpacingValue(
            mobile,
            tablet,
            desktop);
    }

    private static bool ShouldNormalizePixelLength(string propertyAlias)
    {
        return string.Equals(propertyAlias, "layout-width-content", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "layout-width-reading", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "radius-sm", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "radius-md", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "radius-lg", StringComparison.Ordinal) ||
               string.Equals(propertyAlias, "radius-full", StringComparison.Ordinal);
    }

    private static string NormalizePixelLength(string value)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return decimal.TryParse(normalized, out _)
            ? $"{normalized}px"
            : normalized;
    }

    private static string ResolveDimensionValue(string rawValue)
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

            var wrappedValue = GetJsonString(root, "$value");
            return string.IsNullOrWhiteSpace(wrappedValue)
                ? rawValue.Trim()
                : wrappedValue;
        }
        catch (JsonException)
        {
            return rawValue.Trim();
        }
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

    private static ValueTokenDefinition CreateDirectValueTokenDefinition(
        IPublishedElement designTokens,
        DirectValuePropertyDefinition definition)
    {
        var rawValue = GetPropertyValue(designTokens, definition.PropertyAlias);
        var value = ResolveDimensionValue(rawValue);

        return new ValueTokenDefinition(
            definition.TokenAlias,
            definition.Label,
            NormalizePixelLength(value));
    }

    private sealed record ResponsivePropertyDefinition(string PropertyAlias, string TokenAlias, string Label);

    private sealed record SemanticColorPropertyDefinition(string PropertyAlias, string TokenAlias, string Label);

    private sealed record SemanticValuePropertyDefinition(string PropertyAlias, string TokenAlias, string Label);

    private sealed record DirectValuePropertyDefinition(string PropertyAlias, string TokenAlias, string Label);

    private sealed record FixedValuePropertyDefinition(string PropertyAlias, string TokenAlias, string Label);
}
