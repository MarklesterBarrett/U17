using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;

namespace Site.Contentment;

public sealed class ColorSwatchListEditor : IContentmentListEditor
{
    public string Name => "Color Swatches";

    public string Description => "Displays Data List items as clickable color swatches.";

    public string Icon => "icon-colorpicker";

    public string Group => "Custom";

    public string PropertyEditorUiAlias => "My.PropertyEditorUi.Contentment.ColorSwatches";

    public Dictionary<string, object>? DefaultConfig => null;

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object>? DefaultValues => null;

    public IEnumerable<ContentmentConfigurationField> Fields => [];

    public bool HasMultipleValues(Dictionary<string, object>? config) => false;
}
