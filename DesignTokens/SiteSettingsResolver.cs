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
    IPublishedContent? GetSiteIdentitySettings();
    IPublishedContent? GetSiteIdentitySettings(IPublishedContent? content);
    IPublishedContent? GetSiteIdentitySettings(Guid tenantKey);
    IPublishedContent? GetHeaderSettings();
    IPublishedContent? GetHeaderSettings(IPublishedContent? content);
    IPublishedContent? GetHeaderSettings(Guid tenantKey);
    IPublishedContent? GetFooterSettings();
    IPublishedContent? GetFooterSettings(IPublishedContent? content);
    IPublishedContent? GetFooterSettings(Guid tenantKey);
    IPublishedContent? GetThemeSettings();
    IPublishedContent? GetThemeSettings(IPublishedContent? content);
    IPublishedContent? GetThemeSettings(Guid tenantKey);
}

public sealed class SiteSettingsResolver : ISiteSettingsResolver
{
    private const string SiteSettingsAlias = "siteSettings";
    private const string SiteIdentitySettingsAlias = "siteIdentitySettings";
    private const string HeaderSettingsAlias = "siteHeaderSettings";
    private const string FooterSettingsAlias = "siteFooterSettings";
    private const string ThemeSettingsAlias = "siteThemeSettings";
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

    public IPublishedContent? GetSiteIdentitySettings() => GetSectionSettings(SiteIdentitySettingsAlias);

    public IPublishedContent? GetSiteIdentitySettings(IPublishedContent? content) =>
        GetSectionSettings(content, SiteIdentitySettingsAlias);

    public IPublishedContent? GetSiteIdentitySettings(Guid tenantKey) =>
        GetSectionSettings(tenantKey, SiteIdentitySettingsAlias);

    public IPublishedContent? GetHeaderSettings() => GetSectionSettings(HeaderSettingsAlias);

    public IPublishedContent? GetHeaderSettings(IPublishedContent? content) =>
        GetSectionSettings(content, HeaderSettingsAlias);

    public IPublishedContent? GetHeaderSettings(Guid tenantKey) =>
        GetSectionSettings(tenantKey, HeaderSettingsAlias);

    public IPublishedContent? GetFooterSettings() => GetSectionSettings(FooterSettingsAlias);

    public IPublishedContent? GetFooterSettings(IPublishedContent? content) =>
        GetSectionSettings(content, FooterSettingsAlias);

    public IPublishedContent? GetFooterSettings(Guid tenantKey) =>
        GetSectionSettings(tenantKey, FooterSettingsAlias);

    public IPublishedContent? GetThemeSettings() => GetSectionSettings(ThemeSettingsAlias);

    public IPublishedContent? GetThemeSettings(IPublishedContent? content) =>
        GetSectionSettings(content, ThemeSettingsAlias);

    public IPublishedContent? GetThemeSettings(Guid tenantKey) =>
        GetSectionSettings(tenantKey, ThemeSettingsAlias);

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

    private IPublishedContent? GetSectionSettings(string sectionAlias)
    {
        var siteSettings = GetSiteSettings();
        return GetSectionSettingsFromSiteSettings(siteSettings, sectionAlias);
    }

    private IPublishedContent? GetSectionSettings(IPublishedContent? content, string sectionAlias)
    {
        var siteSettings = GetSiteSettings(content);
        return GetSectionSettingsFromSiteSettings(siteSettings, sectionAlias);
    }

    private IPublishedContent? GetSectionSettings(Guid tenantKey, string sectionAlias)
    {
        var siteSettings = GetSiteSettings(tenantKey);
        return GetSectionSettingsFromSiteSettings(siteSettings, sectionAlias);
    }

    private static IPublishedContent? GetSectionSettingsFromSiteSettings(IPublishedContent? siteSettings, string sectionAlias)
    {
        if (siteSettings is null)
        {
            return null;
        }

        return siteSettings
            .Children()
            .FirstOrDefault(x => string.Equals(x.ContentType.Alias, sectionAlias, StringComparison.Ordinal));
    }
}
