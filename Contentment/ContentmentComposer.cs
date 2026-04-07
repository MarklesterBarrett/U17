using Microsoft.Extensions.DependencyInjection;
using Site.DesignTokens;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Contentment.DataEditors;

namespace Site.Contentment;

public sealed class ContentmentComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IDesignTokenStyleRenderer, DesignTokenStyleRenderer>();
        builder.Services.AddUnique<IContentmentDataSource, DesignTokenColorDataSource>();
        builder.Services.AddUnique<IContentmentDataSource, DesignTokenSpacingDataSource>();
        builder.Services.AddUnique<IContentmentListEditor, ColorSwatchListEditor>();
    }
}
