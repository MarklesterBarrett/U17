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
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IDesignTokenProvider, CmsDesignTokenProvider>();
        builder.Services.AddSingleton<IDesignTokenCssGenerator, DesignTokenCssGenerator>();
        builder.Services.AddSingleton<IDesignTokenCssCache, DesignTokenCssCache>();
        builder.Services.AddSingleton<IDesignTokenStyleRenderer, DesignTokenStyleRenderer>();
        builder.Services.AddUnique<IContentmentDataSource, DesignTokenColorDataSource>();
        builder.Services.AddUnique<IContentmentDataSource, DesignTokenSpacingDataSource>();
        builder.Services.AddUnique<IContentmentListEditor, ColorSwatchListEditor>();
        builder.AddNotificationHandler<Umbraco.Cms.Core.Notifications.ContentPublishedNotification, DesignTokenCacheInvalidationHandler>();
        builder.AddNotificationHandler<Umbraco.Cms.Core.Notifications.ContentUnpublishedNotification, DesignTokenCacheInvalidationHandler>();
        builder.AddNotificationHandler<Umbraco.Cms.Core.Notifications.ContentDeletedNotification, DesignTokenCacheInvalidationHandler>();
    }
}
