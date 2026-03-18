using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.DependencyInjection;

namespace site.Composers;

public class SiteSettingsDocumentTypeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SiteSettingsDocumentTypeSeeder>();
}
