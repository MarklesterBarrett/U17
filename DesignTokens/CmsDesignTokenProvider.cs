using System.Text.Json;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Site.DesignTokens;

public sealed class CmsDesignTokenProvider : IDesignTokenProvider
{
    private const string StyleSettingsAlias = "styleSettings";
    private const string LegacyDesignTokensAlias = "designTokens";
    private const string BaseColorsAlias = "baseColors";
    private const string BaseColorItemsAlias = "colours";
    private const string LegacyPrimitiveColorsAlias = "primitiveColors";
    private const string SemanticColorsAlias = "semanticColors";
    private const string SpacingsAlias = "spacings";
    private const string SpacingTokensAlias = "spacingTokens";
    private const string BaseColorAliasProperty = "baseColorAlias";
    private const string LegacyPrimitiveAliasProperty = "primitiveAlias";
    private static readonly IReadOnlyList<(string PropertyAlias, string TokenAlias, string Label)> CoreSpacingProperties =
    [
        ("spacingXs", "space-xs", "xs"),
        ("spacingSm", "space-sm", "sm"),
        ("spacingMd", "space-md", "md"),
        ("spacingLg", "space-lg", "lg"),
        ("spacingXl", "space-xl", "xl"),
        ("spacing2Xl", "space-2xl", "2xl")
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
        ("radiusSm", "radius-sm", "Small"),
        ("radiusMd", "radius-md", "Medium"),
        ("radiusLg", "radius-lg", "Large"),
        ("radiusFull", "radius-full", "Full"),
        ("radiusPill", "radius-pill", "Pill"),
        ("fontFamilySans", "font-family-sans", "Sans Family"),
        ("fontFamilyDisplay", "font-family-display", "Display Family"),
        ("fontSizeSm", "font-size-sm", "Small Size"),
        ("fontSizeBase", "font-size-base", "Base Size"),
        ("fontSizeLg", "font-size-lg", "Large Size"),
        ("fontSizeXl", "font-size-xl", "XL Size"),
        ("fontSize2Xl", "font-size-2xl", "2XL Size"),
        ("lineHeightTight", "line-height-tight", "Tight Line Height"),
        ("lineHeightBase", "line-height-base", "Base Line Height"),
        ("layoutGutter", "layout-gutter", "Layout Gutter"),
        ("layoutWidthContent", "layout-width-content", "Content Width"),
        ("layoutWidthReading", "layout-width-reading", "Reading Width"),
        ("layoutHeaderHeight", "layout-header-height", "Header Height"),
        ("shadowRaised", "shadow-raised", "Raised Shadow"),
        ("shadowLifted", "shadow-lifted", "Lifted Shadow"),
        ("shadowFocus", "shadow-focus", "Focus Shadow")
    ];
    private readonly ISiteSettingsResolver _siteSettingsResolver;

    public CmsDesignTokenProvider(ISiteSettingsResolver siteSettingsResolver)
    {
        _siteSettingsResolver = siteSettingsResolver;
    }

    public DesignTokenSet GetTokens()
    {
        var siteSettings = _siteSettingsResolver.GetSiteSettings();
        var designTokens = GetDesignTokens(siteSettings);

        if (designTokens is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(siteSettings, designTokens),
            GetSpacingTokens(siteSettings),
            GetValueTokens(designTokens));
    }

    public DesignTokenSet GetTokens(Guid tenantKey)
    {
        var siteSettings = _siteSettingsResolver.GetSiteSettings(tenantKey);
        var designTokens = GetDesignTokens(siteSettings);

        if (designTokens is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(siteSettings, designTokens),
            GetSpacingTokens(siteSettings),
            GetValueTokens(designTokens));
    }

    private static IPublishedElement? GetDesignTokens(IPublishedContent? siteSettings)
    {
        return siteSettings
            ?.Value<BlockListItem>(StyleSettingsAlias)
            ?.Content
            ?? siteSettings
            ?.Value<BlockListItem>(LegacyDesignTokensAlias)
            ?.Content;
    }

    private static IReadOnlyList<ColorTokenDefinition> GetColorTokens(IPublishedContent? siteSettings, IPublishedElement designTokens)
    {
        var primitiveColors = GetPrimitiveColors(siteSettings, designTokens);
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

    private static IReadOnlyDictionary<string, string> GetPrimitiveColors(IPublishedContent? siteSettings, IPublishedElement designTokens)
    {
        var primitiveColorBlocks = siteSettings
            ?.Value<BlockListItem>(BaseColorsAlias)
            ?.Content
            .Value<IEnumerable<BlockListItem>>(BaseColorItemsAlias);

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = siteSettings?.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);
        }

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = designTokens.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);
        }

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = designTokens.Value<IEnumerable<BlockListItem>>(LegacyPrimitiveColorsAlias);
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

        if (string.IsNullOrWhiteSpace(primitiveAlias))
        {
            primitiveAlias = token.Value<string>(LegacyPrimitiveAliasProperty)?.Trim() ?? string.Empty;
        }

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

    private static IReadOnlyList<SpacingTokenDefinition> GetSpacingTokens(IPublishedContent? siteSettings)
    {
        var spacings = siteSettings
            ?.Value<BlockListItem>(SpacingsAlias)
            ?.Content;

        if (spacings is null)
        {
            return EnsureZeroSpacingToken([]);
        }

        var coreTokens = CoreSpacingProperties
            .Select(definition => CreateCoreSpacingToken(spacings, definition.PropertyAlias, definition.TokenAlias, definition.Label))
            .Where(token => token is not null)
            .Select(token => token!)
            .ToList();

        var additionalTokens = (spacings.Value<IEnumerable<BlockListItem>>(SpacingTokensAlias) ?? Enumerable.Empty<BlockListItem>())
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

    private static IReadOnlyList<ValueTokenDefinition> GetValueTokens(IPublishedElement designTokens)
    {
        var builtInTokens = new[]
        {
            new ValueTokenDefinition("radius-none", "None", "0")
        };

        var fixedTokens = FixedValueProperties
            .Select(definition => new ValueTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                designTokens.Value<string>(definition.PropertyAlias)?.Trim() ?? string.Empty))
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
}
