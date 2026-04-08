using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace Site.DesignTokens;

public sealed class CmsDesignTokenProvider : IDesignTokenProvider
{
    private const string SiteSettingsAlias = "siteSettings";
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
    private static readonly IReadOnlyList<(string TokenAlias, string Label, string MobilePropertyAlias, string TabletPropertyAlias, string LaptopPropertyAlias, string DesktopPropertyAlias)> FixedSpacingProperties =
    [
        ("space-xs", "XSmall", "spaceXsMobile", "spaceXsTablet", "spaceXsLaptop", "spaceXsDesktop"),
        ("space-sm", "Small", "spaceSmMobile", "spaceSmTablet", "spaceSmLaptop", "spaceSmDesktop"),
        ("space-md", "Medium", "spaceMdMobile", "spaceMdTablet", "spaceMdLaptop", "spaceMdDesktop"),
        ("space-lg", "Large", "spaceLgMobile", "spaceLgTablet", "spaceLgLaptop", "spaceLgDesktop"),
        ("space-xl", "XLarge", "spaceXlMobile", "spaceXlTablet", "spaceXlLaptop", "spaceXlDesktop")
    ];
    private static readonly IReadOnlyList<(string PropertyAlias, string TokenAlias, string Label)> FixedValueProperties =
    [
        ("radiusNone", "radius-none", "None"),
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
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CmsDesignTokenProvider(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public DesignTokenSet GetTokens()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
        var siteSettings = publishedContentQuery
            .ContentAtRoot()
            .FirstOrDefault(x => string.Equals(x.ContentType.Alias, SiteSettingsAlias, StringComparison.Ordinal));

        if (siteSettings is null)
        {
            return new DesignTokenSet([], [], []);
        }

        return new DesignTokenSet(
            GetColorTokens(siteSettings),
            GetSpacingTokens(siteSettings),
            GetValueTokens(siteSettings));
    }

    private static IReadOnlyList<ColorTokenDefinition> GetColorTokens(IPublishedContent siteSettings)
    {
        return FixedColorProperties
            .Select(definition => new ColorTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                siteSettings.Value<string>(definition.PropertyAlias)?.Trim() ?? string.Empty))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value))
            .ToList();
    }

    private static IReadOnlyList<SpacingTokenDefinition> GetSpacingTokens(IPublishedContent siteSettings)
    {
        return FixedSpacingProperties
            .Select(definition => new SpacingTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                siteSettings.Value<string>(definition.MobilePropertyAlias)?.Trim() ?? string.Empty,
                siteSettings.Value<string>(definition.TabletPropertyAlias)?.Trim() ?? string.Empty,
                siteSettings.Value<string>(definition.LaptopPropertyAlias)?.Trim() ?? string.Empty,
                siteSettings.Value<string>(definition.DesktopPropertyAlias)?.Trim() ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Mobile) &&
                        !string.IsNullOrWhiteSpace(x.Tablet) &&
                        !string.IsNullOrWhiteSpace(x.Laptop) &&
                        !string.IsNullOrWhiteSpace(x.Desktop))
            .ToList();
    }

    private static IReadOnlyList<ValueTokenDefinition> GetValueTokens(IPublishedContent siteSettings)
    {
        var fixedTokens = FixedValueProperties
            .Select(definition => new ValueTokenDefinition(
                definition.TokenAlias,
                definition.Label,
                siteSettings.Value<string>(definition.PropertyAlias)?.Trim() ?? string.Empty))
            .Where(token => !string.IsNullOrWhiteSpace(token.Value));

        var additionalTokens = (siteSettings.Value<IEnumerable<BlockListItem>>("additionalTokens") ?? Enumerable.Empty<BlockListItem>())
            .Select(block => block.Content)
            .Where(content => content is not null)
            .Select(content => new ValueTokenDefinition(
                content!.Value<string>("alias")?.Trim() ?? string.Empty,
                content.Value<string>("label")?.Trim() ?? string.Empty,
                content.Value<string>("value")?.Trim() ?? string.Empty))
            .Where(token => !string.IsNullOrWhiteSpace(token.Alias) &&
                            !string.IsNullOrWhiteSpace(token.Label) &&
                            !string.IsNullOrWhiteSpace(token.Value));

        return fixedTokens
            .Concat(additionalTokens)
            .ToList();
    }
}
