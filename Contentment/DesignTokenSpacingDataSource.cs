using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;
using Site.DesignTokens;

namespace Site.Contentment;

public sealed class DesignTokenSpacingDataSource : IContentmentDataSource
{
    private readonly IDesignTokenProvider _designTokenProvider;

    public DesignTokenSpacingDataSource(IDesignTokenProvider designTokenProvider)
    {
        _designTokenProvider = designTokenProvider;
    }

    public string Name => "Style Setting Spacing";

    public string Description => "Reads spacing tokens from site settings.";

    public string Icon => "icon-autofill";

    public string Group => "Custom";

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object> DefaultValues => new();

    public IEnumerable<ContentmentConfigurationField> Fields => [];

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config)
    {
        return _designTokenProvider
            .GetTokens()
            .Spacing
            .Select(x => new DataListItem
            {
                Name = x.Label,
                Value = x.Alias,
                Description = $"{x.Mobile} / {x.Tablet} / {x.Desktop}"
            });
    }
}
