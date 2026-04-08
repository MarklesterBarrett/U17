using Microsoft.Extensions.Caching.Memory;

namespace Site.DesignTokens;

public sealed class DesignTokenCssCache : IDesignTokenCssCache
{
    private const string CacheKey = "site.design-tokens.css";
    private readonly IMemoryCache _memoryCache;
    private readonly IDesignTokenCssGenerator _generator;

    public DesignTokenCssCache(IMemoryCache memoryCache, IDesignTokenCssGenerator generator)
    {
        _memoryCache = memoryCache;
        _generator = generator;
    }

    public string GetCss()
    {
        return _memoryCache.GetOrCreate(CacheKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return _generator.GenerateCss();
        }) ?? string.Empty;
    }

    public void Invalidate()
    {
        _memoryCache.Remove(CacheKey);
    }
}
