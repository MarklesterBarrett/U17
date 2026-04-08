using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Site.DesignTokens;

public sealed class DesignTokenCacheInvalidationHandler :
    INotificationHandler<ContentPublishedNotification>,
    INotificationHandler<ContentUnpublishedNotification>,
    INotificationHandler<ContentDeletedNotification>
{
    private const string SiteSettingsAlias = "siteSettings";
    private readonly IDesignTokenCssCache _cache;

    public DesignTokenCacheInvalidationHandler(IDesignTokenCssCache cache)
    {
        _cache = cache;
    }

    public void Handle(ContentPublishedNotification notification)
    {
        if (notification.PublishedEntities.Any(IsSiteSettings))
        {
            _cache.Invalidate();
        }
    }

    public void Handle(ContentUnpublishedNotification notification)
    {
        if (notification.UnpublishedEntities.Any(IsSiteSettings))
        {
            _cache.Invalidate();
        }
    }

    public void Handle(ContentDeletedNotification notification)
    {
        if (notification.DeletedEntities.Any(IsSiteSettings))
        {
            _cache.Invalidate();
        }
    }

    private static bool IsSiteSettings(Umbraco.Cms.Core.Models.IContent content) =>
        string.Equals(content.ContentType.Alias, SiteSettingsAlias, StringComparison.Ordinal);
}
