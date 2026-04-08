using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;
using Site.DesignTokens;

namespace Site.Contentment;

public sealed class DesignTokenColorDataSource : IContentmentDataSource
{
    private readonly IDesignTokenProvider _designTokenProvider;

    public DesignTokenColorDataSource(IDesignTokenProvider designTokenProvider)
    {
        _designTokenProvider = designTokenProvider;
    }

    public string Name => "Design Token Colors";

    public string Description => "Reads color tokens from site settings.";

    public string Icon => "icon-colorpicker";

    public string Group => "Custom";

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object> DefaultValues => new();

    public IEnumerable<ContentmentConfigurationField> Fields => [];

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config)
    {
        return _designTokenProvider
            .GetTokens()
            .Colors
            .Select(x => new DataListItem
            {
                Name = x.Label,
                Value = x.Alias,
                Description = x.Value
            });
    }
}
