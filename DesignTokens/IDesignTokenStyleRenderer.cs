namespace Site.DesignTokens;

public interface IDesignTokenStyleRenderer
{
    ElementStyleOverrides GetElementStyleOverrides(Umbraco.Cms.Core.Models.PublishedContent.IPublishedElement? settings);
}
