using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services.Filters;

namespace Site.Composers;

public sealed class TenantContentTypeFilterComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.ContentTypeFilters().Append<TenantContentTypeFilter>();
}

public sealed class TenantContentTypeFilter : IContentTypeFilter
{
    private static readonly Guid TenantSiteKey = new("b2142119-0dfb-4a8f-a371-775fefcf7598");
    private static readonly Guid SiteSettingsKey = new("88c20fb1-fbed-41fb-b7e7-23fa8b13db89");
    private const string TenantSiteAlias = "tenantSite";
    private const string PageAlias = "page";
    private const string SiteSettingsAlias = "siteSettings";
    private const string SiteIdentitySettingsAlias = "siteIdentitySettings";
    private const string SiteHeaderSettingsAlias = "siteHeaderSettings";
    private const string SiteFooterSettingsAlias = "siteFooterSettings";
    private const string SiteThemeSettingsAlias = "siteThemeSettings";
    private const string AppliedThemeStylesAlias = "appliedThemeStyles";

    public Task<IEnumerable<T>> FilterAllowedAtRootAsync<T>(IEnumerable<T> contentTypes)
        where T : IContentTypeComposition
    {
        var filtered = contentTypes
            .Where(x => string.Equals(x.Alias, TenantSiteAlias, StringComparison.Ordinal));

        return Task.FromResult(filtered);
    }

    public Task<IEnumerable<ContentTypeSort>> FilterAllowedChildrenAsync(
        IEnumerable<ContentTypeSort> contentTypes,
        Guid parentContentTypeKey,
        Guid? parentContentKey)
    {
        if (parentContentTypeKey == TenantSiteKey)
        {
            var filtered = contentTypes.Where(x =>
                string.Equals(x.Alias, PageAlias, StringComparison.Ordinal) ||
                string.Equals(x.Alias, SiteSettingsAlias, StringComparison.Ordinal));

            return Task.FromResult(filtered);
        }

        if (parentContentTypeKey == SiteSettingsKey)
        {
            var filtered = contentTypes.Where(x =>
                string.Equals(x.Alias, SiteIdentitySettingsAlias, StringComparison.Ordinal) ||
                string.Equals(x.Alias, SiteHeaderSettingsAlias, StringComparison.Ordinal) ||
                string.Equals(x.Alias, SiteFooterSettingsAlias, StringComparison.Ordinal) ||
                string.Equals(x.Alias, SiteThemeSettingsAlias, StringComparison.Ordinal) ||
                string.Equals(x.Alias, AppliedThemeStylesAlias, StringComparison.Ordinal));

            return Task.FromResult(filtered);
        }

        return Task.FromResult(Enumerable.Empty<ContentTypeSort>());
    }
}
