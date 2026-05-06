using Site.DesignTokens;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;
using Umbraco.Extensions;

namespace Site.Contentment;

public sealed class BaseColorDataSource : IContentmentDataSource
{
    private const string StyleSettingsAlias = "styleSettings";
    private const string LegacyDesignTokensAlias = "designTokens";
    private const string BaseColorsAlias = "baseColors";
    private const string LegacyPrimitiveColorsAlias = "primitiveColors";
    private readonly ISiteSettingsResolver _siteSettingsResolver;

    public BaseColorDataSource(ISiteSettingsResolver siteSettingsResolver)
    {
        _siteSettingsResolver = siteSettingsResolver;
    }

    public string Name => "Base Colours";

    public string Description => "Reads base colours from site settings.";

    public string Icon => "icon-colorpicker";

    public string Group => "Custom";

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object> DefaultValues => new();

    public IEnumerable<ContentmentConfigurationField> Fields => [];

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config)
    {
        var siteSettings = _siteSettingsResolver.GetSiteSettings();
        var primitiveColorBlocks = siteSettings?.Value<IEnumerable<BlockListItem>>(BaseColorsAlias);

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = siteSettings?.Value<IEnumerable<BlockListItem>>(LegacyPrimitiveColorsAlias);
        }

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = siteSettings
                ?.Value<BlockListItem>(StyleSettingsAlias)
                ?.Content
                .Value<IEnumerable<BlockListItem>>(BaseColorsAlias);
        }

        if (primitiveColorBlocks is null || !primitiveColorBlocks.Any())
        {
            primitiveColorBlocks = siteSettings
                ?.Value<BlockListItem>(LegacyDesignTokensAlias)
                ?.Content
                .Value<IEnumerable<BlockListItem>>(LegacyPrimitiveColorsAlias);
        }

        if (primitiveColorBlocks is null)
        {
            return [];
        }

        return primitiveColorBlocks
            .Select(x => x.Content)
            .Where(x => x is not null)
            .Select(x => new
            {
                Alias = ResolveAlias(x!),
                ColorValue = ResolveValue(x)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Alias) &&
                        !string.IsNullOrWhiteSpace(x.ColorValue))
            .Select(x => new DataListItem
            {
                Name = x.Alias,
                Value = x.Alias,
                Description = x.ColorValue
            });
    }

    private static string ResolveValue(IPublishedElement token)
    {
        var customValue = token.Value<string>("customValue")?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(customValue))
        {
            return customValue;
        }

        var paletteAlias = token.Value<string>("paletteValue")?.Trim() ?? string.Empty;
        return BuiltInBaseColorDataSource.TryGetColorValue(paletteAlias, out var paletteValue)
            ? paletteValue
            : string.Empty;
    }

    private static string ResolveAlias(IPublishedElement token)
    {
        var paletteAlias = token.Value<string>("paletteValue")?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(paletteAlias))
        {
            return paletteAlias;
        }

        return token.Value<string>("label")?.Trim() ?? string.Empty;
    }
}

public sealed class PrimitiveColorTokenDataSource : IContentmentDataSource
{
    private readonly BaseColorDataSource _inner;

    public PrimitiveColorTokenDataSource(ISiteSettingsResolver siteSettingsResolver)
    {
        _inner = new BaseColorDataSource(siteSettingsResolver);
    }

    public string Name => _inner.Name;

    public string Description => _inner.Description;

    public string Icon => _inner.Icon;

    public string Group => _inner.Group;

    public OverlaySize OverlaySize => _inner.OverlaySize;

    public Dictionary<string, object> DefaultValues => _inner.DefaultValues;

    public IEnumerable<ContentmentConfigurationField> Fields => _inner.Fields;

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config) => _inner.GetItems(config);
}
