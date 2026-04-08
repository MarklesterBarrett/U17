using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Site.DesignTokens;

public interface ISiteSettingsResolver
{
    IPublishedContent? GetSiteSettings();
    IPublishedContent? GetSiteSettings(IPublishedContent? content);
    IPublishedContent? GetSiteSettings(Guid tenantKey);
}

public sealed class SiteSettingsResolver : ISiteSettingsResolver
{
    private const string SiteSettingsAlias = "siteSettings";
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public SiteSettingsResolver(IServiceScopeFactory serviceScopeFactory, IUmbracoContextAccessor umbracoContextAccessor)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    public IPublishedContent? GetSiteSettings()
    {
        if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            var currentContent = umbracoContext?.PublishedRequest?.PublishedContent;
            var contextualSiteSettings = GetSiteSettings(currentContent);

            if (contextualSiteSettings is not null)
            {
                return contextualSiteSettings;
            }
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();

        return publishedContentQuery
            .ContentAtRoot()
            .SelectMany(root =>
            {
                if (string.Equals(root.ContentType.Alias, SiteSettingsAlias, StringComparison.Ordinal))
                {
                    return new[] { root };
                }

                return root.Children()
                    .Where(x => string.Equals(x.ContentType.Alias, SiteSettingsAlias, StringComparison.Ordinal));
            })
            .FirstOrDefault();
    }

    public IPublishedContent? GetSiteSettings(IPublishedContent? content)
    {
        var tenantRoot = content?.Root();
        return GetSiteSettingsFromTenantRoot(tenantRoot);
    }

    public IPublishedContent? GetSiteSettings(Guid tenantKey)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var publishedContentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
        var tenantRoot = publishedContentQuery.Content(tenantKey);
        return GetSiteSettingsFromTenantRoot(tenantRoot);
    }

    private static IPublishedContent? GetSiteSettingsFromTenantRoot(IPublishedContent? tenantRoot)
    {
        if (tenantRoot is null)
        {
            return null;
        }

        return tenantRoot
            .Children()
            .FirstOrDefault(x => string.Equals(x.ContentType.Alias, SiteSettingsAlias, StringComparison.Ordinal));
    }
}
